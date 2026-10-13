using System.Globalization;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Frames;
using TableSimulator.Contracts.Recordings;
using TableSimulator.Contracts.State;
using TableSimulator.Contracts.Statistics;

namespace TableSimulator.Mcp;

/// <summary>Kontrakty API na strucne vysledky nastrojov - len to, co model potrebuje, bez kodov znakov a profilov.</summary>
public static class McpMapper
{
    /// <summary>Najviac dni archivu v zozname dostupnych.</summary>
    public const int ArchiveDays = 31;

    public static StatusResult ToStatus(StateDto state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var boardsPerLine = state.Boards.CountBy(b => b.Line).ToDictionary();
        return new StatusResult(
            state.Lines.Select(l => new LineSummary(l.Number, l.Name, l.TableClass, l.Endpoint, l.Status, l.StatusDetail, l.StatusSince, l.LastReceivedAt,
                boardsPerLine.GetValueOrDefault(l.Number), l.Stats.Frames, l.Stats.BadFrames)).ToArray(),
            state.Boards.Where(HasIssue).Select(b => new BoardIssue(b.Id, b.Name, b.Fault.Kind, b.PoweredOff, b.Mismatch)).ToArray());
    }

    /// <summary>Spravy z <paramref name="log" /> podla filtra - najnovsich <paramref name="limit" />.</summary>
    public static FramesResult ToFrames(IReadOnlyList<FrameDto> log, long after, int? line, int? address, bool problemsOnly, int limit)
    {
        ArgumentNullException.ThrowIfNull(log);
        var matching = log.Where(f => f.Seq > after && (line is null || f.Line == line) && (address is null || f.Address == address)
                                      && (!problemsOnly || f.Status != FrameStatus.Ok)).ToArray();
        var next = log.Count > 0 ? Math.Max(after, log[^1].Seq) : after;
        return new FramesResult(matching.TakeLast(limit).ToArray(), next, matching.Length > limit);
    }

    /// <summary>
    /// Tabule so stavom a textom; verzia je z textu (patri k riadkom, ktore model vidi - s nou caka na zmenu).
    /// <paramref name="texts" /> null = bez textu.
    /// </summary>
    public static BoardsResult ToBoards(IEnumerable<BoardDto> boards, IReadOnlyList<BoardTextDto>? texts)
    {
        ArgumentNullException.ThrowIfNull(boards);
        var byId = texts?.ToDictionary(t => t.Id, StringComparer.Ordinal);
        return new BoardsResult(boards.Select(b =>
        {
            var text = byId?.GetValueOrDefault(b.Id);
            return new BoardSummary(b.Id, b.Name, b.Line, b.Address, b.Manufacturer, b.Display.Family, text?.Version ?? b.Version, b.PoweredOff, b.Fault.Kind,
                b.Stats.LastFrameAt, text?.Rows);
        }).ToArray());
    }

    /// <summary>Podrobnosti tabule; <paramref name="raw" /> = aj profil a obsah s pismami a kodmi znakov.</summary>
    public static BoardDetail ToBoardDetail(BoardDto board, BoardTextDto? text, bool raw)
    {
        ArgumentNullException.ThrowIfNull(board);
        return new BoardDetail(board.Id, board.Name, board.Line, board.Address, board.SecondAddress, board.Manufacturer, board.Display.Family, board.Design,
            board.AutoCreated, board.PoweredOff, board.Fault, board.Mismatch, board.Stats, board.Display.Brightness, board.Display.Clock,
            text?.Version ?? board.Version, text?.Rows ?? [], raw ? board.Profile : null, raw ? board.Display : null);
    }

    /// <summary>
    /// Porovnanie zaznamov: pocty podla vysledku zo vsetkych tabul, zoznam len vybranej tabule a pri
    /// <paramref name="onlyDifferences" /> bez rovnakych tabul a krokov; kroky tabule najviac <paramref name="maxSteps" />.
    /// </summary>
    public static ComparisonResult ToComparison(RecordingComparisonDto comparison, string? board, bool onlyDifferences, int maxSteps)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        var counts = comparison.Boards.CountBy(b => b.Result).ToDictionary();
        var truncated = new List<string>();
        var boards = comparison.Boards
            .Where(b => (board is null || b.Id == board) && (!onlyDifferences || b.Result != BoardDifference.Same))
            .Select(b =>
            {
                var steps = onlyDifferences ? b.Steps.Where(s => s.Kind != ComparisonStepKind.Same).ToList() : b.Steps;
                if (steps.Count <= maxSteps)
                    return b with { Steps = steps };
                truncated.Add(b.Id);
                return b with { Steps = steps.Take(maxSteps).ToList() };
            }).ToList();
        return new ComparisonResult(comparison.A, comparison.B, counts.GetValueOrDefault(BoardDifference.Same), counts.GetValueOrDefault(BoardDifference.Different),
            counts.GetValueOrDefault(BoardDifference.OnlyA), counts.GetValueOrDefault(BoardDifference.OnlyB), boards, truncated);
    }

    /// <summary>Stavy tabule v zazname: v case <paramref name="time" /> (posledny stav, ktory vtedy uz platil) alebo prvych <paramref name="limit" />.</summary>
    public static RecordingBoardResult ToRecordingBoard(BoardTimelineDto timeline, double? time, int limit)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        if (time is { } at)
            return new RecordingBoardResult(timeline.Id, timeline.Name, timeline.Duration,
                timeline.States.LastOrDefault(s => s.Time <= at) is { } state ? [state] : [], false);
        return new RecordingBoardResult(timeline.Id, timeline.Name, timeline.Duration, timeline.States.Take(limit).ToList(), timeline.States.Count > limit);
    }

    /// <summary>Dni archivu od najnovsieho, najviac <see cref="ArchiveDays" />.</summary>
    public static IReadOnlyList<DateOnly> RecentDays(IEnumerable<DateOnly> days) => days.OrderDescending().Take(ArchiveDays).ToList();

    /// <summary>Statistika: sucty po linkach a tabulach za cele obdobie, priebeh len pri <paramref name="series" />.</summary>
    public static StatisticsResult ToStatistics(TrafficStatisticsDto statistics, bool series)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        var lines = statistics.Lines.GroupBy(p => p.Line).OrderBy(g => g.Key).Select(g => new LineTrafficTotal(g.Key,
            g.Sum(p => p.Ok), g.Sum(p => p.BadChecksum), g.Sum(p => p.NoBoard), g.Sum(p => p.Noise), g.Sum(p => p.Other), g.Sum(p => p.Replies),
            g.Sum(p => p.Faults), g.Sum(p => p.BytesIn), g.Sum(p => p.BytesOut), g.Sum(p => p.Connections), g.Sum(p => p.Disconnections))).ToList();
        var boards = statistics.Boards.GroupBy(p => (p.Line, p.Address)).OrderBy(g => g.Key.Line).ThenBy(g => g.Key.Address).Select(g => new BoardTrafficTotal(
            string.Create(CultureInfo.InvariantCulture, $"{g.Key.Line}-{g.Key.Address}"), g.Key.Line, g.Key.Address,
            g.Sum(p => p.Frames), g.Sum(p => p.Acks), g.Sum(p => p.Naks), g.Sum(p => p.Unanswered), g.Max(p => p.MaxGapMs))).ToList();
        return new StatisticsResult(statistics.From, statistics.To, statistics.BucketMinutes, statistics.RetentionDays, lines, boards, statistics.StatusChanges,
            series ? statistics.Lines : null);
    }

    private static bool HasIssue(BoardDto board) => board.Fault.Kind != FaultKind.None || board.PoweredOff || board.Mismatch is not null;
}
