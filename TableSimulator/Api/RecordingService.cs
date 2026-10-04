using System.Collections.Concurrent;
using System.Globalization;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Api;

/// <summary>Zaznam v zozname.</summary>
public sealed record RecordingInfo(string Name, long Size, DateTimeOffset Created, double Duration, int[] Lines, int Frames, string? Note);

/// <summary>Stav prebiehajuceho zaznamu.</summary>
public sealed record RecorderStatusDto(bool Recording, string? Name, DateTimeOffset? Started, long Entries, long Bytes);

/// <summary>Zoznam zaznamov so stavom zaznamu a prehravania.</summary>
public sealed record RecordingsDto(RecordingInfo[] Items, RecorderStatusDto Recorder, ReplayStatus? Replay);

/// <summary>
/// Zaznamy komunikacie v priecinku <c>recordings</c> nastaveni: zaznam prevadzky liniek a jej prehratie do simulatora.
/// </summary>
public sealed class RecordingService : IAsyncDisposable
{
    private readonly Simulator _simulator;
    private readonly TransportManager _transport;
    private readonly LiveHub _hub;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly ConcurrentDictionary<string, (DateTime Stamp, long Size, RecordingInfo Info)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _ticker;
    private SessionRecorder? _recorder;
    private string? _recorderName;
    private ReplayPlayer? _player;

    public RecordingService(Simulator simulator, TransportManager transport, LiveHub hub)
    {
        _simulator = simulator;
        _transport = transport;
        _hub = hub;
        _ticker = new Timer(_ => PublishRecorder(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Priecinok zaznamov.</summary>
    public static string Folder => Path.Combine(AppSettings.DataFolder, "recordings");

    /// <summary>Stav prebiehajuceho zaznamu.</summary>
    public RecorderStatusDto RecorderStatus => _recorder is { } r
        ? new RecorderStatusDto(true, _recorderName, r.Started, r.Entries, r.Bytes)
        : new RecorderStatusDto(false, null, null, 0, 0);

    /// <summary>Stav prehravania; null = este nic neprehravalo.</summary>
    public ReplayStatus? ReplayStatus => _player?.Status;

    /// <summary>Zoznam zaznamov od najnovsieho, so stavom.</summary>
    public RecordingsDto List()
    {
        var items = new List<RecordingInfo>();
        if (Directory.Exists(Folder))
        {
            foreach (var path in Directory.EnumerateFiles(Folder, "*" + RecordingFile.Extension))
            {
                if (Info(path) is { } info)
                    items.Add(info);
            }
        }

        return new RecordingsDto(items.OrderByDescending(i => i.Created).ToArray(), RecorderStatus, ReplayStatus);
    }

    /// <summary>Cesta k zaznamu podla mena; null = zle meno.</summary>
    public static string? PathFor(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains("..", StringComparison.Ordinal))
            return null;
        return Path.Combine(Folder, name + RecordingFile.Extension);
    }

    /// <summary>Zacne zaznam prevadzky vsetkych liniek.</summary>
    public async Task<RecorderStatusDto> StartAsync(string? note)
    {
        await _sync.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_recorder == null)
            {
                Directory.CreateDirectory(Folder);
                var now = _simulator.Now;
                var name = UniqueName(now.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture));
                var stream = new FileStream(PathFor(name)!, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                _recorder = new SessionRecorder(stream, _simulator.ToConfig(), now, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
                _recorderName = name;
                _simulator.StartRecording(_recorder);
                _ticker.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
        }
        finally
        {
            _sync.Release();
        }

        PublishRecorder();
        return RecorderStatus;
    }

    /// <summary>Ukonci zaznam.</summary>
    public async Task<RecorderStatusDto> StopAsync()
    {
        await _sync.WaitAsync().ConfigureAwait(false);
        try
        {
            _ticker.Change(Timeout.Infinite, Timeout.Infinite);
            if (_simulator.StopRecording() is { } recorder)
                await recorder.DisposeAsync().ConfigureAwait(false);
            _recorder = null;
            _recorderName = null;
        }
        finally
        {
            _sync.Release();
        }

        PublishRecorder();
        _hub.Broadcast(new { type = "recordings" });
        return RecorderStatus;
    }

    /// <summary>
    /// Prehra zaznam: doplni linky a tabule zo zaznamu, ktore v simulatore chybaju, a posiela spravy INISSu do liniek.
    /// </summary>
    /// <returns>stav prehravania; null = zaznam neexistuje</returns>
    public async Task<ReplayStatus?> PlayAsync(string name, double speed, bool loop)
    {
        if (PathFor(name) is not { } path || !File.Exists(path))
            return null;

        var recording = await Task.Run(() => RecordingFile.Load(path)).ConfigureAwait(false);
        await StopReplayAsync().ConfigureAwait(false);
        if (_simulator.AddMissing(recording.Header.Config))
            await _transport.SyncAsync().ConfigureAwait(false);

        var player = new ReplayPlayer(_simulator, recording, name, speed, loop);
        player.Progress += status => _hub.Broadcast(new { type = "replay", status });
        _player = player;
        player.Start();
        return player.Status;
    }

    /// <summary>Zastavi prehravanie.</summary>
    public async Task StopReplayAsync()
    {
        if (_player is { } player)
            await player.StopAsync().ConfigureAwait(false);
    }

    /// <summary>Odstrani zaznam (nie prave nahravany).</summary>
    public bool Delete(string name)
    {
        if (PathFor(name) is not { } path || !File.Exists(path) || name == _recorderName)
            return false;
        File.Delete(path);
        _cache.TryRemove(path, out _);
        return true;
    }

    /// <summary>Ulozi zaznam z ineho pocitaca; zly subor odmietne.</summary>
    /// <returns>zaznam; null = subor nie je zaznam simulatora</returns>
    public async Task<RecordingInfo?> UploadAsync(string fileName, Stream body)
    {
        using var buffer = new MemoryStream();
        await body.CopyToAsync(buffer).ConfigureAwait(false);
        try
        {
            buffer.Position = 0;
            RecordingFile.ReadHeader(buffer);
        }
        catch (FormatException)
        {
            return null;
        }

        Directory.CreateDirectory(Folder);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            baseName = baseName.Replace(c, '_');
        var name = UniqueName(string.IsNullOrWhiteSpace(baseName) ? "zaznam" : baseName);
        var path = PathFor(name)!;
        await File.WriteAllBytesAsync(path, buffer.ToArray()).ConfigureAwait(false);
        _hub.Broadcast(new { type = "recordings" });
        return Info(path);
    }

    private static string UniqueName(string name)
    {
        var candidate = name;
        for (var i = 2; File.Exists(PathFor(candidate)); i++)
            candidate = string.Create(CultureInfo.InvariantCulture, $"{name}-{i}");
        return candidate;
    }

    private RecordingInfo? Info(string path)
    {
        var file = new FileInfo(path);
        if (_cache.TryGetValue(path, out var cached) && cached.Stamp == file.LastWriteTimeUtc && cached.Size == file.Length)
            return cached.Info;

        try
        {
            var recording = RecordingFile.Load(path);
            var info = new RecordingInfo(Path.GetFileNameWithoutExtension(path), file.Length, recording.Header.Created, recording.Duration,
                recording.Lines.ToArray(), recording.Entries.Count(e => e.Direction == FrameDirection.Received), recording.Header.Note);
            _cache[path] = (file.LastWriteTimeUtc, file.Length, info);
            return info;
        }
        catch (Exception e) when (e is IOException or FormatException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void PublishRecorder() => _hub.Broadcast(new { type = "recorder", status = RecorderStatus });

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        if (_player != null)
            await _player.DisposeAsync().ConfigureAwait(false);
        await _ticker.DisposeAsync().ConfigureAwait(false);
        _sync.Dispose();
    }
}
