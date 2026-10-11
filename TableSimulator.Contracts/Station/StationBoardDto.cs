namespace TableSimulator.Contracts.Station;

/// <summary>Fyzicka tabula stanice INISSu.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa (ID z TPhysic)</param>
/// <param name="Manufacturer">kluc vyrobcu (<c>MANUFACTURER_KEY</c> katalogu)</param>
/// <param name="Name">nazov tabule (KEY z TPhysic)</param>
public sealed record StationBoardDto(int Line, int Address, string Manufacturer, string Name = "")
{
    /// <summary>Listova tabula: moduly a zoznamy listov z katalogu a TabTab; null = nie je listova.</summary>
    public Boards.FlapProfileDto? Flaps { get; init; }

    /// <summary>Listova tabula: pocet zaznamov (MAX_REC_COUNT).</summary>
    public int FlapRecords { get; init; } = 1;

    /// <summary>Co o cele tabule vedia data stanice; null = nic (tabula bez cela).</summary>
    public StationFaceDto? Face { get; init; }
}
