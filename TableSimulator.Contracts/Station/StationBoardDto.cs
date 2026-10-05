namespace TableSimulator.Contracts.Station;

/// <summary>Fyzicka tabula stanice INISSu.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa (ID z TPhysic)</param>
/// <param name="Manufacturer">kluc vyrobcu (<c>MANUFACTURER_KEY</c> katalogu)</param>
/// <param name="Name">nazov tabule (KEY z TPhysic)</param>
public sealed record StationBoardDto(int Line, int Address, string Manufacturer, string Name = "");
