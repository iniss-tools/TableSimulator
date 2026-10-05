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
    IReadOnlyList<string> SkippedBoards);
