namespace TableSimulator.Application.Settings;

/// <summary>Databaza, s ktorou server bezi (dodava server).</summary>
/// <param name="Provider"><c>sqlite</c> alebo <c>postgres</c></param>
/// <param name="Description">subor SQLite alebo <c>PostgreSQL host:port/databaza (pouzivatel)</c> - bez hesla</param>
public sealed record DatabaseInfo(string Provider, string Description);
