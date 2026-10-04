using System.Text.Json.Serialization;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Stav prehravania.
/// </summary>
public enum ReplayState
{
    [JsonStringEnumMemberName("running")] Running,
    [JsonStringEnumMemberName("finished")] Finished,
    [JsonStringEnumMemberName("stopped")] Stopped
}

/// <summary>
/// Priebeh prehravania pre rozhranie.
/// </summary>
/// <param name="Name">meno zaznamu</param>
/// <param name="State">stav</param>
/// <param name="Position">kde v zazname prehravanie je [s]</param>
/// <param name="Duration">dlzka zaznamu [s]</param>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Loop">po skonceni zacat znova</param>
/// <param name="Pass">kolke prehratie (pri opakovani)</param>
public sealed record ReplayStatus(string Name, ReplayState State, double Position, double Duration, double Speed, bool Loop, int Pass);

/// <summary>
/// Prehrava zaznam do liniek simulatora, akoby spravy posielal INISS: bajty od INISSu v povodnom case (krat rychlost),
/// odpovede tabul sa zahodia. Prehrata prevadzka sa do prebiehajuceho zaznamu nezapisuje.
/// </summary>
public sealed class ReplayPlayer : IAsyncDisposable
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(250);

    private readonly Simulator _simulator;
    private readonly RecordingFile _recording;
    private readonly TimeProvider _time;
    private readonly CancellationTokenSource _cts = new();
    private Task? _task;
    private double _position;
    private int _pass;
    private ReplayState _state = ReplayState.Running;

    /// <param name="simulator">kam sa prehrava</param>
    /// <param name="recording">zaznam</param>
    /// <param name="name">meno zaznamu na zobrazenie</param>
    /// <param name="speed">rychlost (1 = povodny cas); 0 = bez cakania</param>
    /// <param name="loop">po skonceni zacat znova</param>
    /// <param name="time">hodiny (testy)</param>
    public ReplayPlayer(Simulator simulator, RecordingFile recording, string name, double speed, bool loop, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(simulator);
        ArgumentNullException.ThrowIfNull(recording);
        _simulator = simulator;
        _recording = recording;
        _time = time ?? TimeProvider.System;
        Name = name;
        Speed = Math.Max(0, speed);
        Loop = loop;
    }

    /// <summary>Priebeh - pri zmene stavu a najviac 4× za sekundu.</summary>
    public event Action<ReplayStatus>? Progress;

    /// <summary>Meno zaznamu.</summary>
    public string Name { get; }

    /// <summary>Rychlost; 0 = bez cakania.</summary>
    public double Speed { get; }

    /// <summary>Po skonceni zacat znova.</summary>
    public bool Loop { get; }

    /// <summary>Aktualny priebeh.</summary>
    public ReplayStatus Status => new(Name, _state, _position, _recording.Duration, Speed, Loop, Math.Max(1, _pass));

    /// <summary>Dokoncenie prehravania (aj zastavenim).</summary>
    public Task Completion => _task ?? Task.CompletedTask;

    /// <summary>Spusti prehravanie na pozadi.</summary>
    public void Start()
    {
        var token = _cts.Token;
        _task ??= Task.Run(() => RunAsync(token), CancellationToken.None);
    }

    private async Task RunAsync(CancellationToken token)
    {
        var lastProgress = DateTimeOffset.MinValue;
        try
        {
            do
            {
                _pass++;
                foreach (var line in _recording.Lines)
                    _simulator.ResetLine(line);

                // bez cakania by opakovanie nikdy neuvolnilo vlakno
                if (Speed == 0)
                    await Task.Yield();

                var start = _time.GetTimestamp();
                foreach (var entry in _recording.Entries)
                {
                    if (entry.Direction != FrameDirection.Received)
                        continue;

                    if (Speed > 0)
                    {
                        var due = TimeSpan.FromSeconds(entry.Time / Speed) - _time.GetElapsedTime(start);
                        if (due > TimeSpan.Zero)
                            await Task.Delay(due, _time, token).ConfigureAwait(false);
                    }

                    token.ThrowIfCancellationRequested();
                    _simulator.Receive(entry.Line, entry.Bytes, TrafficSource.Replay);
                    _position = entry.Time;

                    var now = _time.GetUtcNow();
                    if (now - lastProgress >= ProgressInterval)
                    {
                        lastProgress = now;
                        Progress?.Invoke(Status);
                    }
                }

                _position = _recording.Duration;
            } while (Loop && !token.IsCancellationRequested);

            // zastavenie medzi dvoma prechodmi opakovania nie je skoncenie
            _state = token.IsCancellationRequested ? ReplayState.Stopped : ReplayState.Finished;
        }
        catch (OperationCanceledException)
        {
            _state = ReplayState.Stopped;
        }

        Progress?.Invoke(Status);
    }

    /// <summary>Zastavi prehravanie.</summary>
    public async Task StopAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        await Completion.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts.Dispose();
    }
}
