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

    /// <summary>
    /// Konzistentna kopia databazy do suboru <paramref name="targetFile" /> pocas behu servera (online zaloha SQLite -
    /// zapisy inych spojeni pockaju alebo sa zopakuju).
    /// </summary>
    public static void BackupTo(string databaseFile, string targetFile)
    {
        using var source = new SqliteConnection(ConnectionString(databaseFile));
        using var target = new SqliteConnection(ConnectionString(targetFile));
        source.Open();
        target.Open();
        source.BackupDatabase(target);
    }

    /// <summary>Migracie, ktore v databaze su a ktore server pozna - na kontrolu zalohy pred obnovou.</summary>
    public static async Task<(IReadOnlyList<string> Applied, IReadOnlyList<string> Known)> MigrationsAsync(string databaseFile,
        CancellationToken cancellationToken = default)
    {
        await using var db = Open(databaseFile);
        var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        return (applied, db.Database.GetMigrations().ToList());
    }

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
