using System.Collections.Concurrent;
using System.Globalization;
using RecordingFileFormat = TableSimulator.Core.Recording.RecordingFile;
using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Recordings;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Recordings;

/// <summary>
/// Zaznamy komunikacie v priecinku zaznamov: zaznam prevadzky liniek a jej prehratie do simulatora.
/// </summary>
public sealed class RecordingService : IAsyncDisposable
{
    private readonly Simulator _simulator;
    private readonly TransportManager _transport;
    private readonly ILiveNotifier _live;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly ConcurrentDictionary<string, (DateTime Stamp, long Size, RecordingInfoDto Info)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _ticker;
    private SessionRecorder? _recorder;
    private string? _recorderName;
    private ReplayPlayer? _player;

    public RecordingService(Simulator simulator, TransportManager transport, ILiveNotifier live, AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _simulator = simulator;
        _transport = transport;
        _live = live;
        Folder = paths.RecordingsFolder;
        _ticker = new Timer(_ => PublishRecorder(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Priecinok zaznamov.</summary>
    public string Folder { get; }

    /// <summary>Stav prebiehajuceho zaznamu.</summary>
    public RecorderStatusDto RecorderStatus => _recorder is { } r
        ? new RecorderStatusDto(true, _recorderName, r.Started, r.Entries, r.Bytes)
        : new RecorderStatusDto(false, null, null, 0, 0);

    /// <summary>Stav prehravania; null = este nic neprehravalo.</summary>
    public ReplayStatusDto? ReplayStatus => _player?.Status is { } status ? DtoMapper.ToDto(status) : null;

    /// <summary>Zoznam zaznamov od najnovsieho, so stavom.</summary>
    public RecordingsDto List()
    {
        var items = new List<RecordingInfoDto>();
        if (Directory.Exists(Folder))
        {
            foreach (var path in Directory.EnumerateFiles(Folder, "*" + RecordingFileFormat.Extension))
            {
                if (Info(path) is { } info)
                    items.Add(info);
            }
        }

        return new RecordingsDto(items.OrderByDescending(i => i.Created).ToArray(), RecorderStatus, ReplayStatus);
    }

    /// <summary>Cesta k zaznamu podla mena; null = zle meno.</summary>
    public string? PathFor(string name) => RecordingNames.IsValid(name) ? Path.Combine(Folder, name + RecordingFileFormat.Extension) : null;

    /// <summary>Zaznam s tymto menom existuje.</summary>
    public bool Exists(string name) => PathFor(name) is { } path && File.Exists(path);

    /// <summary>Do zaznamu s tymto menom sa prave nahrava.</summary>
    public bool IsRecording(string name) => string.Equals(name, _recorderName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Zacne zaznam prevadzky vsetkych liniek (ak uz nebezi).</summary>
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
        _live.Publish(new RecordingsMessage());
        return RecorderStatus;
    }

    /// <summary>
    /// Prehra zaznam: doplni linky a tabule zo zaznamu, ktore v simulatore chybaju, a posiela spravy INISSu do liniek.
    /// </summary>
    /// <param name="name">zaznam</param>
    /// <param name="speed">rychlost; 0 = bez cakania</param>
    /// <param name="loop">dokola</param>
    /// <param name="position">odkial [s]; od ineho miesta nez od zaciatku sa zaznam najprv prehra naprazdno</param>
    /// <param name="paused">len pripravit na mieste a pozastavit</param>
    /// <returns>stav prehravania; null = zaznam neexistuje</returns>
    public async Task<ReplayStatusDto?> PlayAsync(string name, double speed, bool loop, double position = 0, bool paused = false)
    {
        if (PathFor(name) is not { } path || !File.Exists(path))
            return null;

        var recording = await Task.Run(() => RecordingFileFormat.Load(path)).ConfigureAwait(false);
        await StopReplayAsync().ConfigureAwait(false);
        if (_simulator.AddMissing(recording.Header.Config))
            await _transport.SyncAsync().ConfigureAwait(false);

        var player = new ReplayPlayer(_simulator, recording, name, speed, loop);
        player.Progress += status => _live.Publish(new ReplayMessage(DtoMapper.ToDto(status)));
        _player = player;
        player.Start(position, paused);
        return DtoMapper.ToDto(player.Status);
    }

    /// <summary>
    /// Zmeni posledne prehravanie: rychlost, opakovanie, posun a pozastavenie. Zastavene prehravanie sa pri posune
    /// alebo pokracovani spusti znova od daneho miesta.
    /// </summary>
    /// <returns>stav prehravania; null = este nic neprehravalo</returns>
    public async Task<ReplayStatusDto?> UpdateReplayAsync(UpdateReplayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_player is not { } player)
            return null;

        var status = player.Status;
        if (status.State == Core.Recording.ReplayState.Stopped)
        {
            if (request.Position is null && request.Paused != false)
                return DtoMapper.ToDto(status);
            return await PlayAsync(player.Name, request.Speed ?? status.Speed, request.Loop ?? status.Loop, request.Position ?? status.Position,
                request.Paused == true).ConfigureAwait(false);
        }

        if (request.Speed is { } speed)
            player.SetSpeed(speed);
        if (request.Loop is { } loop)
            player.SetLoop(loop);
        if (request.Position is { } position)
            await player.SeekAsync(position).ConfigureAwait(false);
        if (request.Paused == true)
            player.Pause();
        else if (request.Paused == false)
            player.Resume();
        return DtoMapper.ToDto(player.Status);
    }

    /// <summary>Hustota sprav INISSu v zazname po <paramref name="buckets" /> usekoch; null = zaznam neexistuje.</summary>
    public async Task<RecordingActivityDto?> ActivityAsync(string name, int buckets)
    {
        if (PathFor(name) is not { } path || !File.Exists(path))
            return null;
        var recording = await Task.Run(() => RecordingFileFormat.Load(path)).ConfigureAwait(false);
        var counts = new int[Math.Max(1, buckets)];
        var duration = recording.Duration;
        foreach (var entry in recording.Entries.Where(e => e.Direction == FrameDirection.Received))
        {
            var index = duration > 0 ? (int)(entry.Time / duration * counts.Length) : 0;
            counts[Math.Clamp(index, 0, counts.Length - 1)]++;
        }

        return new RecordingActivityDto(duration, counts);
    }

    /// <summary>Zastavi prehravanie.</summary>
    public async Task StopReplayAsync()
    {
        if (_player is { } player)
            await player.StopAsync().ConfigureAwait(false);
    }

    /// <summary>Odstrani zaznam (volajuci overi, ze existuje a nenahrava sa).</summary>
    public void Delete(string name)
    {
        if (PathFor(name) is not { } path)
            return;
        File.Delete(path);
        _cache.TryRemove(path, out _);
        _live.Publish(new RecordingsMessage());
    }

    /// <summary>Ulozi zaznam z ineho pocitaca; subor, ktory nie je zaznam simulatora, odmietne.</summary>
    /// <returns>zaznam; null = subor nie je zaznam simulatora</returns>
    public async Task<RecordingInfoDto?> UploadAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            using var buffer = new MemoryStream(content, false);
            RecordingFileFormat.ReadHeader(buffer);
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
        await File.WriteAllBytesAsync(path, content, cancellationToken).ConfigureAwait(false);
        _live.Publish(new RecordingsMessage());
        return Info(path);
    }

    private string UniqueName(string name)
    {
        var candidate = name;
        for (var i = 2; File.Exists(PathFor(candidate)); i++)
            candidate = string.Create(CultureInfo.InvariantCulture, $"{name}-{i}");
        return candidate;
    }

    private RecordingInfoDto? Info(string path)
    {
        var file = new FileInfo(path);
        if (_cache.TryGetValue(path, out var cached) && cached.Stamp == file.LastWriteTimeUtc && cached.Size == file.Length)
            return cached.Info;

        try
        {
            var recording = RecordingFileFormat.Load(path);
            var info = new RecordingInfoDto(Path.GetFileNameWithoutExtension(path), file.Length, recording.Header.Created, recording.Duration,
                recording.Lines.ToArray(), recording.Entries.Count(e => e.Direction == FrameDirection.Received), recording.Header.Note);
            _cache[path] = (file.LastWriteTimeUtc, file.Length, info);
            return info;
        }
        catch (Exception e) when (e is IOException or FormatException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void PublishRecorder() => _live.Publish(new RecorderMessage(RecorderStatus));

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
