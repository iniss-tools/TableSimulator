namespace TableSimulator.Contracts.Lines;

/// <summary>Linka pre webove rozhranie.</summary>
/// <param name="InissTablePort">hodnota TablePort, ktoru treba zadat v INISSe; prazdna pri seriovej linke</param>
public sealed record LineDto(
    int Number,
    string Name,
    int TableClass,
    string Endpoint,
    LineMode Mode,
    bool AutoLearn,
    string Manufacturer,
    LineStatus Status,
    string? StatusDetail,
    string InissTablePort,
    LineStatsDto Stats,
    long Version)
{
    /// <summary>Seriovy port zrkadla; null = bez zrkadla.</summary>
    public string? Mirror { get; init; }

    /// <summary>Chyba zrkadla; null = v poriadku.</summary>
    public string? MirrorError { get; init; }
}
