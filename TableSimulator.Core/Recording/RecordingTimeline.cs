using TableSimulator.Core.Protocols;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Prehratie zaznamu bez cakania do vlastneho simulatora (bez liniek a bez vplyvu na beziaci simulator) s hodinami
/// podla casu zaznamu: priebeh textu tabul alebo stav tabule v case.
/// </summary>
public static class RecordingTimeline
{
    /// <summary>
    /// Zmeny, medzi ktorymi ubehne najviac tolko [s], su jedna zmena - INISS posiela tabuli riadky po jednom, polovicne
    /// prepisana tabula nie je stav, ktory treba porovnat.
    /// </summary>
    public const double SettleSeconds = 1.0;

    /// <summary>
    /// Kazda zmena textu kazdej tabule v zazname; zmeny tesne za sebou (<see cref="SettleSeconds" />) sa zlucia do jednej
    /// s casom jej zaciatku.
    /// </summary>
    public static IReadOnlyList<BoardTimeline> Build(RecordingFile recording)
    {
        ArgumentNullException.ThrowIfNull(recording);
        var boards = new Dictionary<string, Tracker>(StringComparer.Ordinal);

        Replay(recording, double.PositiveInfinity, (simulator, time) => simulator.Read(s =>
        {
            foreach (var board in s.Boards)
            {
                if (!boards.TryGetValue(board.Id, out var tracker))
                    boards[board.Id] = tracker = new Tracker();
                tracker.Name = board.Name;
                // text sa sklada len pri zmene tabule (aj pocitadla - text sa potom porovna)
                if (tracker.Version == board.Version)
                    continue;
                tracker.Version = board.Version;
                tracker.Change(new BoardState(time, BoardText.Rows(board)));
            }

            return 0;
        }));

        return
        [
            .. boards.Where(b => b.Value.Flush().Count > 0)
                .OrderBy(b => b.Key, BoardIdComparer.Instance)
                .Select(b => new BoardTimeline(b.Key, b.Value.Name, b.Value.States))
        ];
    }

    /// <summary>Stavy jednej tabule: rozpracovana zmena sa zapise, az ked sa tabula na chvilu ustali.</summary>
    private sealed class Tracker
    {
        private BoardState? _pending;
        private double _lastChange;

        public string Name { get; set; } = "";
        public long Version { get; set; } = -1;
        public List<BoardState> States { get; } = [];

        public void Change(BoardState state)
        {
            if (_pending is not null && state.Time - _lastChange <= SettleSeconds)
                _pending = state with { Time = _pending.Time };
            else
            {
                Flush();
                _pending = state;
            }

            _lastChange = state.Time;
        }

        public List<BoardState> Flush()
        {
            if (_pending is { } pending)
            {
                _pending = null;
                // bez zmeny oproti poslednemu stavu (aj prazdna tabula na zaciatku) nie je novy stav
                var unchanged = States.Count > 0 ? States[^1].Key == pending.Key : pending.Rows.All(r => r.Length == 0);
                if (!unchanged)
                    States.Add(pending);
            }

            return States;
        }
    }

    /// <summary>Simulator po prehrati zaznamu az po cas <paramref name="until" /> (vratane).</summary>
    public static Simulator ReplayUntil(RecordingFile recording, double until)
    {
        ArgumentNullException.ThrowIfNull(recording);
        return Replay(recording, until, null);
    }

    private static Simulator Replay(RecordingFile recording, double until, Action<Simulator, double>? afterEach)
    {
        var clock = new ReplayClock(recording.Header.Created);
        var simulator = new Simulator(clock);
        simulator.Load(recording.Header.Config);
        foreach (var entry in recording.Entries)
        {
            if (entry.Time > until)
                break;
            if (entry.Direction != FrameDirection.Received)
                continue;
            clock.Now = recording.Header.Created + TimeSpan.FromSeconds(entry.Time);
            simulator.Receive(entry.Line, entry.Bytes, TrafficSource.Replay);
            afterEach?.Invoke(simulator, entry.Time);
        }

        return simulator;
    }

    /// <summary>Hodiny prehratia - cas spravy v zazname.</summary>
    private sealed class ReplayClock(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
