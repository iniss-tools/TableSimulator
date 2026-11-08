using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols;

namespace TableSimulator.Core.Statistics;

/// <summary>
/// Pocitadla komunikacie od posledneho odberu - simulator ich plni pod svojim zamkom, server ich kazdu minutu odoberie
/// a ulozi. Linky sa pocitaju priamo zo sprav a bajtov, tabule z rozdielu ich suhrnnych pocitadiel od posledneho odberu.
/// </summary>
internal sealed class TrafficCollector
{
    private readonly Dictionary<int, Counts> _lines = [];
    private readonly Dictionary<string, (long Frames, long Acks, long Naks, long Unanswered)> _boards = new(StringComparer.Ordinal);
    private readonly List<LineStatusChange> _changes = [];

    private sealed class Counts
    {
        public int Ok;
        public int BadChecksum;
        public int NoBoard;
        public int Noise;
        public int Other;
        public int Replies;
        public int Faults;
        public long BytesIn;
        public long BytesOut;
        public int Connections;
        public int Disconnections;
    }

    private Counts Line(int line)
    {
        if (!_lines.TryGetValue(line, out var counts))
            _lines[line] = counts = new Counts();
        return counts;
    }

    public void Bytes(int line, long received, long sent)
    {
        var counts = Line(line);
        counts.BytesIn += received;
        counts.BytesOut += sent;
    }

    public void Frame(int line, FrameDirection direction, FrameStatus status)
    {
        var counts = Line(line);
        if (direction == FrameDirection.Sent)
        {
            if (status == FrameStatus.Fault)
                counts.Faults++;
            else
                counts.Replies++;
            return;
        }

        switch (status)
        {
            case FrameStatus.Ok:
                counts.Ok++;
                break;
            case FrameStatus.BadChecksum:
                counts.BadChecksum++;
                break;
            case FrameStatus.NoBoard:
                counts.NoBoard++;
                break;
            case FrameStatus.Noise:
                counts.Noise++;
                break;
            default:
                counts.Other++;
                break;
        }
    }

    /// <summary>Zmena stavu linky; pripojenie a odpojenie INISSu sa aj pocita.</summary>
    public void Status(int line, LineStatus previous, LineStatus status, string? detail, DateTimeOffset time)
    {
        if (status == previous)
            return;
        _changes.Add(new LineStatusChange(line, time, status, detail));
        if (status == LineStatus.Connected)
            Line(line).Connections++;
        else if (status == LineStatus.Lost && previous == LineStatus.Connected)
            Line(line).Disconnections++;
    }

    /// <summary>
    /// Odoberie pocitadla do minuty <paramref name="minute" />: linky s nejakou komunikaciou a tabule, ktorym od posledneho
    /// odberu pribudli spravy alebo odpovede (rozdiel suhrnnych pocitadiel; tabula zmenena alebo nova zacina od nuly).
    /// </summary>
    public TrafficSample Take(long minute, IEnumerable<SimBoard> boards)
    {
        var lines = _lines.Select(l => new LineMinute(l.Key, minute, l.Value.Ok, l.Value.BadChecksum, l.Value.NoBoard, l.Value.Noise, l.Value.Other,
            l.Value.Replies, l.Value.Faults, l.Value.BytesIn, l.Value.BytesOut, l.Value.Connections, l.Value.Disconnections)).ToList();
        _lines.Clear();

        var boardMinutes = new List<BoardMinute>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var board in boards)
        {
            seen.Add(board.Id);
            var s = board.Stats;
            var now = (s.Frames, s.Acks, s.Naks, s.Unanswered);
            // pocitadla mohli klesnut (nova tabula s rovnakou adresou) - potom od nuly
            var last = _boards.TryGetValue(board.Id, out var l) && l.Frames <= now.Frames && l.Acks <= now.Acks && l.Naks <= now.Naks
                       && l.Unanswered <= now.Unanswered
                ? l
                : (0, 0, 0, 0);
            _boards[board.Id] = now;
            var gap = s.TakeMaxGap();
            if (now == last && gap is null)
                continue;
            boardMinutes.Add(new BoardMinute(board.Line, board.Address, minute, (int)(now.Frames - last.Frames), (int)(now.Acks - last.Acks),
                (int)(now.Naks - last.Naks), (int)(now.Unanswered - last.Unanswered), gap));
        }

        foreach (var removed in _boards.Keys.Where(k => !seen.Contains(k)).ToList())
            _boards.Remove(removed);

        var changes = _changes.ToList();
        _changes.Clear();
        return new TrafficSample(lines, boardMinutes, changes);
    }
}
