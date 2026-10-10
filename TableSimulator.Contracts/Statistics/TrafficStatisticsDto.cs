namespace TableSimulator.Contracts.Statistics;

/// <summary>
/// Statistika komunikacie za obdobie (GET /api/statistics): intervaly s nejakou komunikaciou (prazdne chybaju) a zmeny
/// stavu liniek.
/// </summary>
/// <param name="From">zaciatok obdobia (zaokruhleny na interval)</param>
/// <param name="To">koniec obdobia</param>
/// <param name="BucketMinutes">dlzka intervalu grafu v minutach</param>
/// <param name="Lines">linky po intervaloch</param>
/// <param name="Boards">tabule po intervaloch</param>
/// <param name="StatusChanges">zmeny stavu liniek v obdobi a posledna pred nim</param>
/// <param name="RetentionDays">ako dlho sa statistika uchovava [dni]</param>
public sealed record TrafficStatisticsDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int BucketMinutes,
    IReadOnlyList<LineTrafficPointDto> Lines,
    IReadOnlyList<BoardTrafficPointDto> Boards,
    IReadOnlyList<LineStatusChangeDto> StatusChanges,
    int RetentionDays);
