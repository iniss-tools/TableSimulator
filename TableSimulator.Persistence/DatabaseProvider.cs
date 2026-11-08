namespace TableSimulator.Persistence;

/// <summary>Poskytovatel databazy simulatora.</summary>
public enum DatabaseProvider
{
    /// <summary>Subor SQLite v priecinku nastaveni (predvolene).</summary>
    Sqlite,

    /// <summary>Server PostgreSQL.</summary>
    Postgres
}
