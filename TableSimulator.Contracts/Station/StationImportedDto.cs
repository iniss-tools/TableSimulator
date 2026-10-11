namespace TableSimulator.Contracts.Station;

/// <summary>Co import stanice v simulatore zmenil.</summary>
/// <param name="LinesUpdated">linky, ktorym sa zmenila trieda alebo port</param>
/// <param name="BoardsKept">tabule, ktore uz v simulatore boli (ich nastavenie ostalo)</param>
/// <param name="UnsupportedLines">linky s triedou, ktoru simulator nepozna (vynechane aj s tabulami)</param>
/// <param name="SkippedBoards">tabule vyrobcu, ktoreho simulator na linke nezobrazi (<c>linka-adresa</c>)</param>
public sealed record StationImportedDto(
    int LinesAdded,
    int LinesUpdated,
    int BoardsAdded,
    int BoardsKept,
    IReadOnlyList<int> UnsupportedLines,
    IReadOnlyList<string> SkippedBoards)
{
    /// <summary>Nove virtualne tabule pre tabule s exportom do XML.</summary>
    public int VirtualBoardsAdded { get; init; }

    /// <summary>Tabule s exportom do XML, pre ktore virtualna tabula uz bola.</summary>
    public int VirtualBoardsKept { get; init; }

    /// <summary>Co sa stalo s priecinkom logov INISSu.</summary>
    public StationLogFolder LogFolder { get; init; }

    /// <summary>Nove predlohy ciel z dat stanice.</summary>
    public int FacesAdded { get; init; }

    /// <summary>Tabule, ktore dostali celo z dat stanice.</summary>
    public int FacesAssigned { get; init; }
}
