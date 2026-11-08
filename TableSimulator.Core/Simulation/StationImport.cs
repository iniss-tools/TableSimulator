namespace TableSimulator.Core.Simulation;

/// <summary>
/// Linky a tabule stanice INISSu, ktore ma simulator mat - posiela ich GVDEditor pri presmerovani liniek INISSu na
/// simulator (cislo a trieda linky z registra, tabule z TPhysic a katalogu).
/// </summary>
public sealed record StationImport
{
    public List<StationLine> Lines { get; init; } = [];
    public List<StationBoard> Boards { get; init; } = [];

    /// <summary>Celo z dat stanice dostanu aj tabule, ktore uz su a celo nemaju (inak len nove tabule).</summary>
    public bool FacesForExisting { get; init; }
}
