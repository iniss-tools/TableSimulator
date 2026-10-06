using System.Text.Json;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Settings;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Settings;
using TableSimulator.Persistence.Simulation;

namespace TableSimulator.Infrastructure.Persistence;

/// <summary>
/// Databaza pri starte servera, este pred jeho zostavenim (porty, pristup zo siete a prihlasovanie su v nastaveni):
/// migracie a nastavenie servera. Prvy start s prazdnou databazou prevezme nastavenie a linky s tabulami zo starych
/// suborov <c>settings.json</c> a <c>config.json</c> priecinka nastaveni (subory sa premenuju na <c>*.imported</c>),
/// inak zacne predvolenym nastavenim a jednou linkou ELEN so samoucenim.
/// </summary>
public static partial class DatabaseBootstrap
{
    /// <summary>Pripona prevedeneho stareho suboru.</summary>
    public const string ImportedSuffix = ".imported";

    /// <summary>Uvedie databazu do poriadku a vrati nastavenie servera.</summary>
    public static async Task<AppSettings> PrepareAsync(AppPaths paths, ILogger logger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(logger);
        await using var db = ServerDatabase.Open(paths.DatabaseFile);
        await ServerDatabase.MigrateAsync(db, cancellationToken).ConfigureAwait(false);
        var settings = new SettingsRepository(db);
        if (settings.Load() is { } stored)
            return stored;

        var imported = new List<string>();
        var legacySettings = ReadLegacy(paths.LegacySettingsFile, json => JsonSerializer.Deserialize<AppSettings>(json, JsonDefaults.Files), imported, logger);
        var legacyConfig = ReadLegacy(paths.LegacyConfigFile, SimulatorConfigJson.Deserialize, imported, logger);
        var config = legacyConfig ?? DefaultConfig();
        var initial = legacySettings ?? new AppSettings();

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
        {
            await new SimulatorConfigRepository(db).SaveAsync(config, cancellationToken).ConfigureAwait(false);
            settings.Save(initial);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        // az po ulozeni do databazy - pri chybe ostanu subory na dalsi pokus
        foreach (var file in imported)
        {
            File.Move(file, file + ImportedSuffix, true);
            Imported(logger, file, file + ImportedSuffix);
        }

        if (legacyConfig is null)
            DefaultLine(logger);
        else
            ConfigImported(logger, config.Lines.Count, config.Boards.Count);
        return initial;
    }

    /// <summary>Linky a tabule noveho simulatora: jedna linka ELEN so samoucenim.</summary>
    public static SimulatorConfig DefaultConfig() => new() { Lines = [new LineConfig(1, 4, LineEndpoint.DefaultFor(1).ToString())] };

    /// <summary>Stary subor; poskodeny sa odlozi ako <c>*.bad</c> a nepouzije.</summary>
    private static T? ReadLegacy<T>(string file, Func<string, T?> parse, List<string> imported, ILogger logger) where T : class
    {
        if (!File.Exists(file))
            return null;
        try
        {
            var value = parse(File.ReadAllText(file));
            imported.Add(file);
            return value;
        }
        catch (JsonException e)
        {
            var backup = file + ".bad";
            File.Move(file, backup, true);
            LegacyBroken(logger, e, file, backup);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Subor {File} prevedeny do databazy, povodny je {Imported}")]
    private static partial void Imported(ILogger logger, string file, string imported);

    [LoggerMessage(Level = LogLevel.Information, Message = "Linky a tabule prevedene do databazy ({Lines} liniek, {Boards} tabul)")]
    private static partial void ConfigImported(ILogger logger, int lines, int boards);

    [LoggerMessage(Level = LogLevel.Information, Message = "Nova databaza - jedna linka ELEN so samoucenim")]
    private static partial void DefaultLine(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stary subor {File} je poskodeny (odlozeny ako {Backup}) - nepouzije sa")]
    private static partial void LegacyBroken(ILogger logger, Exception exception, string file, string backup);
}
