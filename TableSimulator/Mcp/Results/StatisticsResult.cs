using System.ComponentModel;
using TableSimulator.Contracts.Statistics;

namespace TableSimulator.Mcp.Results;

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
