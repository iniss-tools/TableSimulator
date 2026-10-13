using System.ComponentModel;
using TableSimulator.Contracts.Statistics;

namespace TableSimulator.Mcp;

/// <summary>Statistika komunikacie za obdobie - sucty po linkach a tabulach, volitelne priebeh.</summary>
public sealed record StatisticsResult(
    DateTimeOffset From,
    DateTimeOffset To,
    [property: Description("Length of one interval of the series [min].")]
    int BucketMinutes,
    [property: Description("How many days the statistics are kept.")]
    int RetentionDays,
    IReadOnlyList<LineTrafficTotal> Lines,
    IReadOnlyList<BoardTrafficTotal> Boards,
    [property: Description("Changes of the line connection status in the period.")]
    IReadOnlyList<LineStatusChangeDto> StatusChanges,
    [property: Description("Per-interval counters of each line; only with series = true.")]
    IReadOnlyList<LineTrafficPointDto>? Series);

/// <summary>Sucty linky za obdobie.</summary>
public sealed record LineTrafficTotal(
    int Line,
    [property: Description("Messages from INISS processed normally.")]
    int Ok,
    int BadChecksum,
    [property: Description("Messages for an address without a board.")]
    int NoBoard,
    [property: Description("Runs of bytes outside messages.")]
    int Noise,
    [property: Description("Other invalid messages.")]
    int Other,
    [property: Description("Board replies without a simulated fault.")]
    int Replies,
    [property: Description("Replies affected by a simulated fault (also omitted ones).")]
    int Faults,
    [property: Description("Bytes from INISS.")]
    long BytesIn,
    [property: Description("Bytes to INISS.")]
    long BytesOut,
    [property: Description("INISS connected to the line.")]
    int Connections,
    [property: Description("INISS disconnected from the line.")]
    int Disconnections);

/// <summary>Sucty tabule za obdobie.</summary>
public sealed record BoardTrafficTotal(
    string Id,
    int Line,
    int Address,
    [property: Description("Messages for the board.")]
    int Frames,
    [property: Description("Positive replies.")]
    int Acks,
    [property: Description("Negative replies (also simulated faults).")]
    int Naks,
    [property: Description("Messages without a reply.")]
    int Unanswered,
    [property: Description("Longest gap between two messages for the board [ms]; null = none.")]
    long? MaxGapMs);
