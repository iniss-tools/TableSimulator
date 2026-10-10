namespace TableSimulator.Contracts.Database;

/// <summary>Databaza simulatora: pouzivana a vybrana v <c>database.json</c> (zmena sa prejavi po restarte servera).</summary>
/// <param name="Active">databaza, ktoru server prave pouziva</param>
/// <param name="ActiveDescription">pouzivana databaza bez hesla: subor SQLite alebo <c>PostgreSQL host:port/databaza (pouzivatel)</c></param>
/// <param name="Provider">vybrana databaza</param>
/// <param name="Postgres">pripojenie k PostgreSQL (ostava ulozene aj pri vybere SQLite)</param>
/// <param name="RestartRequired">vyber sa lisi od pouzivanej databazy - prejavi sa po restarte servera</param>
/// <param name="Error">vyber sa neda precitat (napr. heslo zasifrovane na inom pocitaci); null = v poriadku</param>
public sealed record DatabaseSettingsDto(
    DatabaseKind Active,
    string ActiveDescription,
    DatabaseKind Provider,
    PostgresSettingsDto Postgres,
    bool RestartRequired,
    string? Error);
