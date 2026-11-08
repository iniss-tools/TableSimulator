namespace TableSimulator.Core.Simulation;

/// <summary>Fyzicka tabula stanice.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa (ID z TPhysic)</param>
/// <param name="Manufacturer">kluc vyrobcu (<c>MANUFACTURER_KEY</c> katalogu)</param>
public sealed record StationBoard(int Line, int Address, string Manufacturer)
{
    /// <summary>Nazov tabule (KEY z TPhysic).</summary>
    public string Name { get; init; } = "";

    /// <summary>Listova tabula: moduly a zoznamy listov z katalogu a TabTab; null = nie je listova alebo bez katalogu.</summary>
    public Boards.Flaps.FlapProfile? Flaps { get; init; }

    /// <summary>Listova tabula: pocet zaznamov (MAX_REC_COUNT) - riadkov je zaznamy × riadky zaznamu.</summary>
    public int FlapRecords { get; init; } = 1;

    /// <summary>Predloha cela z dat stanice (<see cref="BoardFace.Id" />); null = bez cela.</summary>
    public string? Face { get; init; }

    /// <summary>Hodnoty premennych cela (kolaj, nastupiste, stanica).</summary>
    public IReadOnlyDictionary<string, string>? FaceValues { get; init; }

    /// <summary>Sirka riadku v bodoch z katalogu (nie listova tabula); 0 = podla vyrobcu.</summary>
    public int Width { get; init; }

    /// <summary>Riadkov tabule z katalogu (nie listova tabula); 0 = podla vyrobcu.</summary>
    public int Rows { get; init; }

    /// <summary>Ustredna AK Signal: svetla a audio linky z dat stanice (snimac a stavy sa neberu); null = ina tabula.</summary>
    public Boards.AkSignal.AkSignalProfile? AkSignal { get; init; }
}
