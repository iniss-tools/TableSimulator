using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Backup;
using TableSimulator.Application.Server;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Backup;
using TableSimulator.Infrastructure.Persistence;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Settings;

namespace TableSimulator.Infrastructure.Backup;

/// <summary>Popis zalohy v ZIP (<c>backup.json</c>).</summary>
/// <param name="Format">vzdy <see cref="ServerBackup.FormatName" /></param>
/// <param name="Version">verzia formatu</param>
/// <param name="Created">kedy vznikla</param>
/// <param name="ServerVersion">verzia servera</param>
/// <param name="Migration">posledna migracia databazy</param>
/// <param name="Designs">pocet vlastnych vzhladov</param>
/// <param name="Media">pocet suborov informacnych panelov</param>
/// <param name="Recordings">pocet zaznamov komunikacie; null = bez zaznamov</param>
public sealed record BackupManifest(string Format, int Version, DateTimeOffset Created, string ServerVersion, string? Migration, int Designs, int Media,
    int? Recordings)
{
    public BackupInfoDto ToDto() => new(Created, ServerVersion, Designs, Media, Recordings);
}

/// <summary>
/// Zaloha celeho simulatora do ZIP: <c>backup.json</c>, <c>tablesimulator.db</c> (online kopia SQLite po ulozeni
/// liniek a tabul), <c>designs/&lt;id&gt;/…</c> (vlastne vzhlady), <c>media/…</c> (subory informacnych panelov)
/// a volitelne <c>recordings/*.tsrec</c>. Obnova zalohu skontroluje (format, databaza simulatora, migracie, ktore server
/// pozna) a rozbali do <c>restore.pending</c> v priecinku nastaveni - pouzije ju <see cref="ApplyPendingAsync" /> pri
/// najblizsom starte servera, skor nez sa databaza otvori. Kluce ochrany dat a certifikaty HTTPS sa nezalohuju.
/// </summary>
public sealed class ServerBackup(AppPaths paths, DatabaseConnection database, ISettingsStore settings, SimulatorConfigStore configStore, IServerHost host,
    TimeProvider time)
    : IServerBackup
{
    public const string FormatName = "TableSimulator.Backup";
    public const int CurrentVersion = 1;

    private const string ManifestEntry = "backup.json";
    private const string DatabaseEntry = "tablesimulator.db";
    private const string DesignsPrefix = "designs/";
    private const string MediaPrefix = "media/";
    private const string RecordingsPrefix = "recordings/";
    private const string DesignFile = "design.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Pripravena obnova v priecinku nastaveni.</summary>
    public static string PendingFolder(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return Path.Combine(paths.DataFolder, "restore.pending");
    }

    /// <summary>Kam sa pri obnove odlozia povodne subory.</summary>
    public static string SavedFolder(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return Path.Combine(paths.DataFolder, "before-restore");
    }

    public async Task<BackupFile> CreateAsync(bool recordings, CancellationToken cancellationToken)
    {
        // linky a tabule sa do databazy ukladaju s odkladom - zaloha ma mat aktualne
        await configStore.SaveAsync().ConfigureAwait(false);
        var now = time.GetLocalNow();
        var path = Path.Combine(Path.GetTempPath(), $"tablesimulator-backup-{Guid.NewGuid():N}.zip");
        var copy = path + ".db";
        var designsFolder = paths.DesignsFolder(settings.Current);
        try
        {
            await SnapshotAsync(copy, cancellationToken).ConfigureAwait(false);
            var applied = (await ServerDatabase.MigrationsAsync(copy, cancellationToken).ConfigureAwait(false)).Applied;
            var migration = applied.Count > 0 ? applied[^1] : null;
            await Task.Run(() =>
            {
                using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
                zip.CreateEntryFromFile(copy, DatabaseEntry, CompressionLevel.Optimal);
                var designs = 0;
                foreach (var dir in Folders(designsFolder).Where(d => File.Exists(Path.Combine(d, DesignFile))))
                {
                    foreach (var file in Directory.GetFiles(dir))
                        AddFile(zip, file, $"{DesignsPrefix}{Path.GetFileName(dir)}/{Path.GetFileName(file)}", CompressionLevel.Optimal);
                    designs++;
                }

                // obrazky a videa su uz skomprimovane
                var media = 0;
                foreach (var file in Files(paths.MediaFolder))
                {
                    AddFile(zip, file, MediaPrefix + Path.GetFileName(file), CompressionLevel.NoCompression);
                    media++;
                }

                int? recorded = null;
                if (recordings)
                {
                    recorded = 0;
                    foreach (var file in Files(paths.RecordingsFolder).Where(f => f.EndsWith(".tsrec", StringComparison.OrdinalIgnoreCase)))
                    {
                        AddFile(zip, file, RecordingsPrefix + Path.GetFileName(file), CompressionLevel.Optimal);
                        recorded++;
                    }
                }

                var manifest = new BackupManifest(FormatName, CurrentVersion, now, host.Version, migration, designs, media, recorded);
                using var stream = zip.CreateEntry(ManifestEntry).Open();
                JsonSerializer.Serialize(stream, manifest, Json);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        finally
        {
            File.Delete(copy);
        }

        return new BackupFile(path, $"tablesimulator-{now:yyyy-MM-dd_HH-mm}.zip");
    }

    /// <summary>
    /// Databaza do suboru SQLite - spolocny format zalohy: SQLite online kopiou, PostgreSQL prenosom vsetkych udajov
    /// do novej databazy SQLite.
    /// </summary>
    private async Task SnapshotAsync(string file, CancellationToken cancellationToken)
    {
        if (database.Provider == DatabaseProvider.Sqlite)
        {
            await Task.Run(() => ServerDatabase.BackupTo(paths.DatabaseFile, file), cancellationToken).ConfigureAwait(false);
            return;
        }

        await CopyToSqliteAsync(database, file, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Vsetky udaje databazy do noveho suboru SQLite.</summary>
    private static async Task CopyToSqliteAsync(DatabaseConnection source, string file, CancellationToken cancellationToken)
    {
        await using var from = ServerDatabase.Open(source);
        await using var to = ServerDatabase.Open(file);
        await ServerDatabase.MigrateAsync(to, cancellationToken).ConfigureAwait(false);
        await DatabaseCopier.CopyAsync(from, to, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<BackupInfoDto>> PrepareRestoreAsync(Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var upload = Path.Combine(Path.GetTempPath(), $"tablesimulator-restore-{Guid.NewGuid():N}.zip");
        var staging = Path.Combine(paths.DataFolder, $"restore.tmp-{Guid.NewGuid():N}");
        try
        {
            if (!await CopyLimitedAsync(content, upload, cancellationToken).ConfigureAwait(false))
                return Result.Failure<BackupInfoDto>(BackupErrors.TooLarge());

            BackupManifest manifest;
            try
            {
                using var zip = ZipFile.OpenRead(upload);
                if (zip.GetEntry(ManifestEntry) is not { } manifestEntry)
                    return Result.Failure<BackupInfoDto>(BackupErrors.Format("backup.json is missing"));
                await using (var stream = manifestEntry.Open())
                    manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(stream, Json, cancellationToken).ConfigureAwait(false)
                               ?? throw new JsonException("empty");
                if (manifest.Format != FormatName || manifest.Version is < 1 or > CurrentVersion)
                    return Result.Failure<BackupInfoDto>(BackupErrors.Format($"format {manifest.Format} {manifest.Version}"));
                if (zip.GetEntry(DatabaseEntry) is null)
                    return Result.Failure<BackupInfoDto>(BackupErrors.Format("the database is missing"));
                if (Extract(zip, staging) is { } bad)
                    return Result.Failure<BackupInfoDto>(BackupErrors.Format($"unexpected entry {bad}"));
            }
            catch (Exception e) when (e is InvalidDataException or JsonException)
            {
                return Result.Failure<BackupInfoDto>(BackupErrors.Format(e.Message));
            }

            try
            {
                var (applied, known) = await ServerDatabase.MigrationsAsync(Path.Combine(staging, DatabaseEntry), cancellationToken).ConfigureAwait(false);
                if (applied.Count == 0)
                    return Result.Failure<BackupInfoDto>(BackupErrors.Format("the database is not a simulator database"));
                if (applied.FirstOrDefault(m => !known.Contains(m)) is { } newer)
                    return Result.Failure<BackupInfoDto>(BackupErrors.Newer(newer));
            }
            catch (SqliteException e)
            {
                return Result.Failure<BackupInfoDto>(BackupErrors.Format(e.Message));
            }

            var pending = PendingFolder(paths);
            if (Directory.Exists(pending))
                Directory.Delete(pending, true);
            Directory.Move(staging, pending);
            return Result.Success(manifest.ToDto());
        }
        finally
        {
            File.Delete(upload);
            if (Directory.Exists(staging))
                Directory.Delete(staging, true);
        }
    }

    public BackupInfoDto? PendingRestore() => ReadManifest(PendingFolder(paths))?.ToDto();

    public RestoreResultDto? LastRestore()
    {
        var file = ResultFile(paths);
        if (!File.Exists(file))
            return null;
        try
        {
            return JsonSerializer.Deserialize<RestoreResultDto>(File.ReadAllText(file), Json);
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            return null;
        }
    }

    /// <summary>Vysledok poslednej obnovy (pri sluzbe Windows sa zaznam zo startu do logu nedostane).</summary>
    private static string ResultFile(AppPaths paths) => Path.Combine(paths.DataFolder, "restore-result.json");

    private static void WriteResult(AppPaths paths, RestoreResultDto result)
    {
        try
        {
            File.WriteAllText(ResultFile(paths), JsonSerializer.Serialize(result, Json));
        }
        catch (IOException)
        {
            // vysledok je aj v logu konzoly
        }
    }

    public bool CancelRestore()
    {
        var pending = PendingFolder(paths);
        if (!Directory.Exists(pending))
            return false;
        Directory.Delete(pending, true);
        return true;
    }

    /// <summary>
    /// Pouzije pripravenu obnovu - pri starte servera, skor nez sa otvori databaza. Povodna databaza, subory panelov
    /// a nahradene vzhlady a zaznamy sa odlozia do <c>before-restore\&lt;cas&gt;</c>; vzhlady a zaznamy, ktore zaloha
    /// nema, ostanu. Pri chybe sa databaza vrati a obnova sa odlozi ako <c>restore.failed-&lt;cas&gt;</c>.
    /// </summary>
    public static async Task ApplyPendingAsync(AppPaths paths, DatabaseConnection database, ILogger logger, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(logger);
        var pending = PendingFolder(paths);
        if (ReadManifest(pending) is not { } manifest)
            return;

        var stamp = now.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
        var saved = Path.Combine(SavedFolder(paths), stamp);
        var savedDatabase = new List<(string Saved, string Original)>();
        try
        {
            Directory.CreateDirectory(saved);
            if (database.Provider == DatabaseProvider.Postgres)
            {
                await RestorePostgresAsync(database, Path.Combine(pending, DatabaseEntry), Path.Combine(saved, DatabaseEntry)).ConfigureAwait(false);
            }
            else
            {
                // databaza aj s pomocnymi subormi SQLite
                foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
                {
                    var file = paths.DatabaseFile + suffix;
                    if (!File.Exists(file))
                        continue;
                    var target = Path.Combine(saved, Path.GetFileName(file));
                    File.Move(file, target);
                    savedDatabase.Add((target, file));
                }

                File.Move(Path.Combine(pending, DatabaseEntry), paths.DatabaseFile);
            }

            // kam patria vzhlady, urcuje obnovene nastavenie
            AppSettings restored;
            await using (var db = ServerDatabase.Open(database))
            {
                await ServerDatabase.MigrateAsync(db).ConfigureAwait(false);
                restored = new SettingsRepository(db).Load() ?? new AppSettings();
            }

            // vzhlady a zaznamy sa pridaju (rovnake sa nahradia), subory panelov patria k databaze - vymenia sa cele
            MergeEntries(Path.Combine(pending, "designs"), paths.DesignsFolder(restored), Path.Combine(saved, "designs"), directories: true);
            ReplaceFolder(Path.Combine(pending, "media"), paths.MediaFolder, Path.Combine(saved, "media"));
            if (manifest.Recordings is not null)
                MergeEntries(Path.Combine(pending, "recordings"), paths.RecordingsFolder, Path.Combine(saved, "recordings"), directories: false);

            Directory.Delete(pending, true);
            BackupLog.Restored(logger, manifest.Created, manifest.ServerVersion, saved);
            WriteResult(paths, new RestoreResultDto(now, manifest.ToDto(), saved, null));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SqliteException or Npgsql.NpgsqlException or DbUpdateException)
        {
            // databaza naspat - server ma nabehnut s povodnymi udajmi
            if (savedDatabase.Count > 0)
            {
                File.Delete(paths.DatabaseFile);
                foreach (var (from, to) in savedDatabase)
                    File.Move(from, to, true);
            }

            var failed = Path.Combine(paths.DataFolder, $"restore.failed-{stamp}");
            Directory.Move(pending, failed);
            BackupLog.RestoreFailed(logger, e, failed);
            WriteResult(paths, new RestoreResultDto(now, manifest.ToDto(), failed, e.Message));
        }
    }

    /// <summary>
    /// Obnova do PostgreSQL: zaloha (SQLite) sa najprv migruje na poslednu verziu, sucasne udaje sa odlozia do suboru
    /// SQLite a vsetko sa prenesie zo zalohy v jednej transakcii - pri chybe ostanu povodne udaje.
    /// </summary>
    private static async Task RestorePostgresAsync(DatabaseConnection database, string backup, string saved)
    {
        await using (var current = ServerDatabase.Open(database))
            await ServerDatabase.MigrateAsync(current).ConfigureAwait(false);
        await CopyToSqliteAsync(database, saved, CancellationToken.None).ConfigureAwait(false);
        await using var from = ServerDatabase.Open(backup);
        await ServerDatabase.MigrateAsync(from).ConfigureAwait(false);
        await using var to = ServerDatabase.Open(database);
        await DatabaseCopier.CopyAsync(from, to).ConfigureAwait(false);
    }

    /// <summary>Rozbali zalohu; vrati meno neocakavanej polozky (cesta mimo priecinka, neznamy obsah), inak null.</summary>
    private static string? Extract(ZipArchive zip, string folder)
    {
        var root = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith('/'))
                continue;
            var known = name is ManifestEntry or DatabaseEntry
                        || name.StartsWith(DesignsPrefix, StringComparison.Ordinal) && name.Count(c => c == '/') == 2
                        || (name.StartsWith(MediaPrefix, StringComparison.Ordinal) || name.StartsWith(RecordingsPrefix, StringComparison.Ordinal))
                        && name.Count(c => c == '/') == 1;
            var target = Path.GetFullPath(Path.Combine(folder, name));
            if (!known || name.Contains("..", StringComparison.Ordinal) || !target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return entry.FullName;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target);
        }

        return null;
    }

    private static async Task<bool> CopyLimitedAsync(Stream content, string path, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > BackupErrors.MaxBytes)
                return false;
            await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>Subor do ZIP - aj prave zapisovany (zaznam komunikacie).</summary>
    private static void AddFile(ZipArchive zip, string file, string name, CompressionLevel level)
    {
        using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var target = zip.CreateEntry(name, level).Open();
        source.CopyTo(target);
    }

    private static string[] Folders(string folder) => Directory.Exists(folder) ? Directory.GetDirectories(folder) : [];

    private static string[] Files(string folder) => Directory.Exists(folder) ? Directory.GetFiles(folder) : [];

    private static BackupManifest? ReadManifest(string folder)
    {
        var file = Path.Combine(folder, ManifestEntry);
        if (!File.Exists(file))
            return null;
        try
        {
            return JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(file), Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Polozky zalohy do priecinka; rovnomenne existujuce sa najprv odlozia.</summary>
    private static void MergeEntries(string source, string target, string saved, bool directories)
    {
        if (!Directory.Exists(source))
            return;
        Directory.CreateDirectory(target);
        var entries = directories ? Directory.GetDirectories(source) : Directory.GetFiles(source);
        foreach (var entry in entries)
        {
            var destination = Path.Combine(target, Path.GetFileName(entry));
            var backup = Path.Combine(saved, Path.GetFileName(entry));
            if (directories)
            {
                if (Directory.Exists(destination))
                    MoveDirectory(destination, backup);
                MoveDirectory(entry, destination);
            }
            else
            {
                if (File.Exists(destination))
                {
                    Directory.CreateDirectory(saved);
                    File.Move(destination, backup, true);
                }

                File.Move(entry, destination);
            }
        }
    }

    /// <summary>Priecinok sa vymeni cely; povodny sa odlozi.</summary>
    private static void ReplaceFolder(string source, string target, string saved)
    {
        if (Directory.Exists(target))
            MoveDirectory(target, saved);
        if (Directory.Exists(source))
            MoveDirectory(source, target);
    }

    /// <summary>Presun priecinka aj na iny disk (kopia a zmazanie).</summary>
    private static void MoveDirectory(string source, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        try
        {
            Directory.Move(source, target);
        }
        catch (IOException) when (!string.Equals(Path.GetPathRoot(source), Path.GetPathRoot(target), StringComparison.OrdinalIgnoreCase))
        {
            CopyDirectory(source, target);
            Directory.Delete(source, true);
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }
}

internal static partial class BackupLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Obnovena zaloha z {Created} (server {Version}); povodne udaje su v {Saved}")]
    public static partial void Restored(ILogger logger, DateTimeOffset created, string version, string saved);

    [LoggerMessage(Level = LogLevel.Error, Message = "Obnova zalohy zlyhala - server bezi s povodnymi udajmi, zaloha je v {Failed}")]
    public static partial void RestoreFailed(ILogger logger, Exception error, string failed);
}
