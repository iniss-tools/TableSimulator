namespace TableSimulator.Core.Simulation;

/// <summary>
/// Linky a tabule stanice INISSu, ktore ma simulator mat - posiela ich GVDEditor pri presmerovani liniek INISSu na
/// simulator (cislo a trieda linky z registra, tabule z TPhysic a katalogu).
/// </summary>
public sealed record StationImport
{
    public List<StationLine> Lines { get; init; } = [];
    public List<StationBoard> Boards { get; init; } = [];
}

/// <summary>Linka stanice.</summary>
/// <param name="Number">cislo linky (podla neho INISS priraduje tabule)</param>
/// <param name="TableClass">trieda linky</param>
/// <param name="Port">port TCP, na ktory INISS posiela (TablePort <c>N=TCP://host:port</c>)</param>
public sealed record StationLine(int Number, int TableClass, int Port)
{
    /// <summary>Nazov pre obsluhu (napr. sekcia Driver0).</summary>
    public string Name { get; init; } = "";
}

/// <summary>Fyzicka tabula stanice.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa (ID z TPhysic)</param>
/// <param name="Manufacturer">kluc vyrobcu (<c>MANUFACTURER_KEY</c> katalogu)</param>
public sealed record StationBoard(int Line, int Address, string Manufacturer)
{
    /// <summary>Nazov tabule (KEY z TPhysic).</summary>
    public string Name { get; init; } = "";

    /// <summary>Listova tabula: moduly a zoznamy listov z katalogu a TabTab; null = nie je listova alebo bez katalogu.</summary>
    public Boards.FlapProfile? Flaps { get; init; }

    /// <summary>Listova tabula: pocet zaznamov (MAX_REC_COUNT) - riadkov je zaznamy × riadky zaznamu.</summary>
    public int FlapRecords { get; init; } = 1;
}

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
    IReadOnlyList<string> SkippedBoards);
