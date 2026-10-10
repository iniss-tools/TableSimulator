namespace TableSimulator.Contracts.Database;

/// <summary>Zmena vyberu databazy (PUT /api/database); prejavi sa po restarte servera.</summary>
public sealed record UpdateDatabaseRequest
{
    public DatabaseKind Provider { get; init; }

    /// <summary>Pripojenie k PostgreSQL - ulozi sa aj pri vybere SQLite.</summary>
    public PostgresSettingsRequest Postgres { get; init; } = new();
}
