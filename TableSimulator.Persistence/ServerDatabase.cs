using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TableSimulator.Persistence.Sqlite;

namespace TableSimulator.Persistence;

/// <summary>
/// Pripojenie k databaze simulatora aj mimo DI - nastavenie servera sa cita pri starte skor, nez sa server zostavi.
/// </summary>
public static class ServerDatabase
{
    /// <summary>Pripojenie k suboru SQLite (bez poolu - subor sa da po skonceni hned presunut alebo zmazat).</summary>
    public static string ConnectionString(string databaseFile) =>
        new SqliteConnectionStringBuilder { DataSource = databaseFile, Pooling = false }.ToString();

    /// <summary>Databaza v subore <paramref name="databaseFile" /> (volajuci ju uvolni).</summary>
    public static AppDbContext Open(string databaseFile) =>
        new SqliteAppDbContext(new DbContextOptionsBuilder<SqliteAppDbContext>().UseSqlite(ConnectionString(databaseFile)).Options);

    /// <summary>Zalozi priecinok a databazu uvedie na poslednu migraciu.</summary>
    public static async Task MigrateAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        var folder = Path.GetDirectoryName(db.Database.GetDbConnection().DataSource);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
