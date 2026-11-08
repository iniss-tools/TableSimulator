namespace TableSimulator.Infrastructure.Persistence;

/// <summary>Obsah <c>database.json</c>: poskytovatel databazy a pripojenie k PostgreSQL.</summary>
public sealed record DatabaseConfigFile
{
    /// <summary><c>sqlite</c> (predvolene) alebo <c>postgres</c>.</summary>
    public string Provider { get; init; } = DatabaseConfig.Sqlite;

    public PostgresConfig? Postgres { get; init; }
}
