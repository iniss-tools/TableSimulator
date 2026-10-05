namespace TableSimulator.Contracts.Virtual;

/// <summary>Subor XML tabule najdeny v priecinkoch logov INISSu. Casy su miestny cas pocitaca s INISSom.</summary>
/// <param name="Name">meno bez pripony (<c>SAVE_XML</c>)</param>
/// <param name="Path">cesta k suboru</param>
/// <param name="Modified">posledna zmena suboru</param>
/// <param name="Station">stanica z XML; null, ak sa subor este nepodarilo precitat</param>
/// <param name="Key">kluc tabule z TPhysic</param>
/// <param name="Written">cas zapisu podla INISSu</param>
/// <param name="ValidTo">koniec platnosti obsahu</param>
/// <param name="Rows">pocet zaznamov</param>
/// <param name="Stale">platnost uplynula - INISS subor neobnovuje (asi nebezi)</param>
/// <param name="Error">preco sa subor neda precitat; null = v poriadku</param>
public sealed record VirtualSourceDto(
    string Name,
    string Path,
    DateTime Modified,
    string? Station,
    string? Key,
    DateTime? Written,
    DateTime? ValidTo,
    int Rows,
    bool Stale,
    string? Error);
