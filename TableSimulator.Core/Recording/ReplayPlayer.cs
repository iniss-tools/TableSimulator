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
    [JsonStringEnumMemberName("paused")] Paused,
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
/// <param name="At">kedy plati poloha - pocas prehravania sa da dopocitat (poloha + uplynuly cas × rychlost)</param>
public sealed record ReplayStatus(string Name, ReplayState State, double Position, double Duration, double Speed, bool Loop, int Pass, DateTimeOffset At);

/// <summary>
/// Prehrava zaznam do liniek simulatora, akoby spravy posielal INISS: bajty od INISSu v povodnom case (krat rychlost),
/// odpovede tabul sa zahodia. Prehrata prevadzka sa do prebiehajuceho zaznamu nezapisuje. Pocas prehravania sa da
/// pozastavit, zmenit rychlost a posunut na lubovolne miesto - tabule liniek zaznamu sa vtedy zmazu a spravy pred tym
/// miestom sa prehraju naprazdno (<see cref="Simulator.FastForward" />). Po dohrati prehravac caka - posun alebo
/// pokracovanie ho znova spusti; zastavenie ho ukonci.
/// </summary>
public sealed class ReplayPlayer : IAsyncDisposable
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(250);

    private readonly Simulator _simulator;
    private readonly RecordingFile _recording;
    private readonly TimeProvider _time;
    private readonly RecordingEntry[] _entries;
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _lock = new();
    private CancellationTokenSource _wake = new();
    private Task? _task;

    // stav chraneny _lock
    private ReplayState _state;
    private double _speed;
    private bool _loop;
    private int _pass = 1;
    private int _index;
    private double _anchorPosition;
    private long _anchorTimestamp;
    private double? _pendingSeek;
    private readonly List<TaskCompletionSource> _seekWaiters = [];

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
        _entries = recording.Entries.Where(e => e.Direction == FrameDirection.Received).ToArray();
        Name = name;
        _speed = Math.Max(0, speed);
        _loop = loop;
        _anchorTimestamp = _time.GetTimestamp();
    }

    /// <summary>Priebeh - pri zmene stavu a pocas prehravania najviac 4× za sekundu.</summary>
    public event Action<ReplayStatus>? Progress;

    /// <summary>Meno zaznamu.</summary>
    public string Name { get; }

    /// <summary>Aktualny priebeh.</summary>
    public ReplayStatus Status
    {
        get
        {
            lock (_lock)
                return StatusCore();
        }
    }

    /// <summary>Dokoncenie prehravania (zastavenim).</summary>
    public Task Completion => _task ?? Task.CompletedTask;

    /// <summary>
    /// Spusti prehravanie na pozadi od <paramref name="position" /> [s]; od ineho miesta nez od zaciatku sa zaznam
    /// najprv prehra naprazdno. <paramref name="paused" /> = pripravit na mieste a cakat.
    /// </summary>
    public void Start(double position = 0, bool paused = false)
    {
        lock (_lock)
        {
            _state = paused ? ReplayState.Paused : ReplayState.Running;
            if (position > 0)
                _pendingSeek = position;
            _anchorTimestamp = _time.GetTimestamp();
        }

        var token = _cts.Token;
        _task ??= Task.Run(() => RunAsync(token), CancellationToken.None);
    }

    /// <summary>Pozastavi prehravanie na aktualnom mieste.</summary>
    public void Pause() => Change(() =>
    {
        if (_state != ReplayState.Running)
            return;
        _anchorPosition = PositionCore();
        _state = ReplayState.Paused;
    });

    /// <summary>Pokracuje v prehravani; dohrany zaznam zacne od zaciatku.</summary>
    public void Resume() => Change(() =>
    {
        if (_state == ReplayState.Finished)
        {
            _pendingSeek = 0;
            _pass++;
        }
        else if (_state != ReplayState.Paused)
        {
            return;
        }

        _state = ReplayState.Running;
        _anchorTimestamp = _time.GetTimestamp();
    });

    /// <summary>Zmeni rychlost (0 = bez cakania) bez skoku v zazname.</summary>
    public void SetSpeed(double speed) => Change(() =>
    {
        _anchorPosition = PositionCore();
        _anchorTimestamp = _time.GetTimestamp();
        _speed = Math.Max(0, speed);
    });

    /// <summary>Zapne alebo vypne opakovanie.</summary>
    public void SetLoop(bool loop) => Change(() => _loop = loop);

    /// <summary>
    /// Posunie prehravanie na <paramref name="position" /> [s] - tabule budu ukazovat obsah z toho miesta; pozastavene
    /// ostane pozastavene, dohrane sa pozastavi na novom mieste. Vrati sa, ked sa posun vykona.
    /// </summary>
    public Task SeekAsync(double position)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Change(() =>
        {
            _pendingSeek = Math.Clamp(position, 0, _recording.Duration);
            _seekWaiters.Add(done);
            if (_state == ReplayState.Finished)
                _state = ReplayState.Paused;
        });
        if (_task is null || _task.IsCompleted)
            done.TrySetResult();
        return done.Task;
    }

    /// <summary>Zmena stavu pod zamkom, prebudenie slucky a oznamenie priebehu.</summary>
    private void Change(Action change)
    {
        ReplayStatus status;
        CancellationTokenSource wake;
        lock (_lock)
        {
            if (_state == ReplayState.Stopped)
                return;
            change();
            status = StatusCore();
            wake = _wake;
            _wake = new CancellationTokenSource();
        }

        // stary zdroj sa neuvolnuje - slucka moze jeho token prave spajat s dalsim
        wake.Cancel();
        Progress?.Invoke(status);
    }

    private ReplayStatus StatusCore() =>
        new(Name, _state, PositionCore(), _recording.Duration, _speed, _loop, _pass, _time.GetUtcNow());

    /// <summary>Kde v zazname prehravanie je - pocas prehravania sa posuva s casom, najviac po dalsiu spravu.</summary>
    private double PositionCore()
    {
        if (_state != ReplayState.Running || _speed == 0)
            return _anchorPosition;
        var next = _index < _entries.Length ? _entries[_index].Time : _recording.Duration;
        var moving = _anchorPosition + _time.GetElapsedTime(_anchorTimestamp).TotalSeconds * _speed;
        return Math.Min(Math.Max(_anchorPosition, moving), Math.Max(next, _anchorPosition));
    }

    private async Task RunAsync(CancellationToken token)
    {
        var lastProgress = DateTimeOffset.MinValue;
        try
        {
            while (!token.IsCancellationRequested)
            {
                double? seek;
                CancellationToken wake;
                ReplayState state;
                RecordingEntry? entry = null;
                TimeSpan due = TimeSpan.Zero;
                var speed = 0.0;
                lock (_lock)
                {
                    speed = _speed;
                    seek = _pendingSeek;
                    _pendingSeek = null;
                    wake = _wake.Token;
                    state = _state;
                    if (seek is null && state == ReplayState.Running)
                    {
                        if (_index < _entries.Length)
                        {
                            entry = _entries[_index];
                            if (_speed > 0)
                                due = TimeSpan.FromSeconds((entry.Time - _anchorPosition) / _speed) - _time.GetElapsedTime(_anchorTimestamp);
                        }
                        else if (_loop && _entries.Length > 0)
                        {
                            _pass++;
                            _index = 0;
                            _anchorPosition = 0;
                            _anchorTimestamp = _time.GetTimestamp();
                            foreach (var line in _recording.Lines)
                                _simulator.ResetLine(line);
                            continue;
                        }
                        else
                        {
                            _state = ReplayState.Finished;
                            _anchorPosition = _recording.Duration;
                        }
                    }
                }

                if (seek is { } target)
                {
                    DoSeek(target);
                    continue;
                }

                if (entry is null)
                {
                    if (state == ReplayState.Running)
                        Progress?.Invoke(Status);
                    // pozastavene alebo dohrane - caka na prikaz
                    await WaitAsync(Timeout.InfiniteTimeSpan, wake, token).ConfigureAwait(false);
                    continue;
                }

                if (due > TimeSpan.Zero)
                {
                    // prikaz (pauza, rychlost, posun) caka prerusi a slucka sa rozhodne znova
                    if (await WaitAsync(due, wake, token).ConfigureAwait(false))
                        continue;
                }
                else if (speed == 0 && (_index & 0xFF) == 0)
                {
                    // bez cakania by prehravanie neuvolnilo vlakno
                    await Task.Yield();
                }

                lock (_lock)
                {
                    // medzitym prisiel prikaz - sprava sa posle az podla neho
                    if (_pendingSeek is not null || _state != ReplayState.Running || _index >= _entries.Length || _entries[_index] != entry)
                        continue;
                    _index++;
                    if (_speed == 0)
                        _anchorPosition = entry.Time;
                }

                token.ThrowIfCancellationRequested();
                _simulator.Receive(entry.Line, entry.Bytes, TrafficSource.Replay);

                var now = _time.GetUtcNow();
                if (now - lastProgress >= ProgressInterval)
                {
                    lastProgress = now;
                    Progress?.Invoke(Status);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // zastavenie
        }

        List<TaskCompletionSource> waiters;
        lock (_lock)
        {
            _anchorPosition = PositionCore();
            _state = ReplayState.Stopped;
            waiters = [.. _seekWaiters];
            _seekWaiters.Clear();
        }

        foreach (var waiter in waiters)
            waiter.TrySetResult();
        Progress?.Invoke(Status);
    }

    /// <summary>Posun: tabule zaznamu zmaze a spravy pred miestom prehra naprazdno, prehravanie pokracuje odtial.</summary>
    private void DoSeek(double target)
    {
        var index = 0;
        while (index < _entries.Length && _entries[index].Time < target)
            index++;
        _simulator.FastForward(_recording.Lines.ToList(), _entries.Take(index).Select(e => (e.Line, e.Bytes)));

        List<TaskCompletionSource> waiters;
        ReplayStatus status;
        lock (_lock)
        {
            _index = index;
            _anchorPosition = target;
            _anchorTimestamp = _time.GetTimestamp();
            waiters = [.. _seekWaiters];
            _seekWaiters.Clear();
            status = StatusCore();
        }

        foreach (var waiter in waiters)
            waiter.TrySetResult();
        Progress?.Invoke(status);
    }

    /// <summary>Caka <paramref name="delay" /> alebo na prikaz; true = prerusene prikazom.</summary>
    private async Task<bool> WaitAsync(TimeSpan delay, CancellationToken wake, CancellationToken token)
    {
        using var both = CancellationTokenSource.CreateLinkedTokenSource(wake, token);
        try
        {
            await Task.Delay(delay, _time, both.Token).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return true;
        }
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
        lock (_lock)
            _wake.Dispose();
    }
}
