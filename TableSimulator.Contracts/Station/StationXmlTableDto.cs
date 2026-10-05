namespace TableSimulator.Contracts.Station;

/// <summary>Tabula INISSu s exportom do XML - podklad virtualnej tabule.</summary>
/// <param name="Name">nazov tabule (KEY z TPhysic)</param>
/// <param name="Source">meno suboru XML bez pripony (SAVE_XML z TPhysic)</param>
public sealed record StationXmlTableDto(string Name, string Source);
