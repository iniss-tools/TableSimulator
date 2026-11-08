namespace TableSimulator.Core.Simulation;

/// <summary>Co import stanice v simulatore zmenil.</summary>
/// <param name="LinesAdded">nove linky</param>
/// <param name="LinesUpdated">linky, ktorym sa zmenila trieda alebo port</param>
/// <param name="BoardsAdded">nove tabule</param>
/// <param name="BoardsKept">tabule, ktore uz v simulatore boli (ich nastavenie ostalo)</param>
/// <param name="UnsupportedLines">linky s triedou, ktoru simulator nepozna (vynechane aj s tabulami)</param>
/// <param name="SkippedBoards">tabule vyrobcu, ktoreho simulator na linke danej triedy nezobrazi (<c>linka-adresa</c>)</param>
public sealed record StationImportResult(
    int LinesAdded,
    int LinesUpdated,
    int BoardsAdded,
    int BoardsKept,
    IReadOnlyList<int> UnsupportedLines,
    IReadOnlyList<string> SkippedBoards)
{
    /// <summary>Predlohy ciel, ktore tabule z importu dostali (jedna polozka na tabulu).</summary>
    public IReadOnlyList<string> FacesAssigned { get; init; } = [];
}
