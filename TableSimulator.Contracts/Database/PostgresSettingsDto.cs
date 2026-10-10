namespace TableSimulator.Contracts.Database;

/// <summary>Pripojenie k PostgreSQL; heslo len ako priznak.</summary>
/// <param name="Host">server</param>
/// <param name="Port">port</param>
/// <param name="Database">databaza</param>
/// <param name="Username">pouzivatel; prazdny = prihlasenie Windows, ak ho server dovoli</param>
/// <param name="PasswordSet">heslo je ulozene (zasifrovane pre tento pocitac)</param>
/// <param name="Options">dalsie parametre pripojenia Npgsql, napr. <c>SSL Mode=Require</c></param>
public sealed record PostgresSettingsDto(string Host, int Port, string Database, string Username, bool PasswordSet, string Options);
