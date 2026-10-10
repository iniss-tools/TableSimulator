using Microsoft.Data.Sqlite;
using Npgsql;

namespace TableSimulator.Persistence;

/// <summary>Poskytovatel databazy simulatora.</summary>
public enum DatabaseProvider
{
    /// <summary>Subor SQLite v priecinku nastaveni (predvolene).</summary>
    Sqlite,

    /// <summary>Server PostgreSQL.</summary>
    Postgres
}

/// <summary>Databaza simulatora: poskytovatel a pripojenie.</summary>
/// <param name="Provider">poskytovatel</param>
/// <param name="ConnectionString">pripojenie (pri SQLite subor bez poolu, pri PostgreSQL aj s heslom - nezapisovat do logov)</param>
public sealed record DatabaseConnection(DatabaseProvider Provider, string ConnectionString)
{
    /// <summary>Subor SQLite.</summary>
    public static DatabaseConnection Sqlite(string databaseFile) => new(DatabaseProvider.Sqlite, ServerDatabase.ConnectionString(databaseFile));

    /// <summary>Pripojenie bez hesla - do logov a rozhrania: subor SQLite alebo <c>PostgreSQL host:port/databaza (pouzivatel)</c>.</summary>
    public string Description => Provider switch
    {
        DatabaseProvider.Sqlite => new SqliteConnectionStringBuilder(ConnectionString).DataSource,
        _ => Postgres(new NpgsqlConnectionStringBuilder(ConnectionString))
    };

    // ToString by vypisal aj heslo
    public override string ToString() => $"{Provider}: {Description}";

    private static string Postgres(NpgsqlConnectionStringBuilder b) =>
        $"PostgreSQL {b.Host}:{b.Port}/{b.Database}" + (string.IsNullOrEmpty(b.Username) ? "" : $" ({b.Username})");
}
