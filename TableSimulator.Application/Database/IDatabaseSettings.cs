using TableSimulator.Contracts.Database;

namespace TableSimulator.Application.Database;

/// <summary>
/// Vyber databazy simulatora (implementuje server - subor <c>database.json</c> mimo databazy): SQLite alebo PostgreSQL.
/// Zmena sa prejavi po restarte servera; udaje sa medzi databazami prenasaju zalohou a obnovou.
/// </summary>
public interface IDatabaseSettings
{
    /// <summary>Pouzivana a vybrana databaza.</summary>
    DatabaseSettingsDto Get();

    /// <summary>Zapise vyber databazy (heslo null = nechat ulozene).</summary>
    void Save(UpdateDatabaseRequest request);

    /// <summary>Skusi pripojenie k PostgreSQL (heslo null = ulozene).</summary>
    Task<DatabaseTestResultDto> TestAsync(PostgresSettingsRequest request, CancellationToken cancellationToken);
}
