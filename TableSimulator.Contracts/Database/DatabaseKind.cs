using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Database;

/// <summary>Databaza simulatora.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DatabaseKind>))]
public enum DatabaseKind
{
    /// <summary>Subor SQLite v priecinku nastaveni (predvolene).</summary>
    [JsonStringEnumMemberName("sqlite")] Sqlite,

    /// <summary>Server PostgreSQL.</summary>
    [JsonStringEnumMemberName("postgres")] Postgres
}
