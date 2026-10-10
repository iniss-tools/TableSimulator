using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using TableSimulator.Application.Settings;
using TableSimulator.Persistence;

namespace TableSimulator.Infrastructure.Persistence;

/// <summary>Pripojenie k PostgreSQL v <c>database.json</c>.</summary>
public sealed record PostgresConfig
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = "tablesimulator";

    /// <summary>Pouzivatel; prazdny = prihlasenie Windows (SSPI/Kerberos), ak ho server PostgreSQL dovoli.</summary>
    public string Username { get; init; } = "";

    /// <summary>Citatelne heslo - napisane rucne; server ho pri starte zasifruje do <see cref="ProtectedPassword" />.</summary>
    public string? Password { get; init; }

    /// <summary>Heslo zasifrovane DPAPI pre tento pocitac (kopia suboru inde nepomoze).</summary>
    public string? ProtectedPassword { get; init; }

    /// <summary>Dalsie parametre pripojenia Npgsql, napr. <c>SSL Mode=Require</c>.</summary>
    public string Options { get; init; } = "";
}

/// <summary>Obsah <c>database.json</c>: poskytovatel databazy a pripojenie k PostgreSQL.</summary>
public sealed record DatabaseConfigFile
{
    /// <summary><c>sqlite</c> (predvolene) alebo <c>postgres</c>.</summary>
    public string Provider { get; init; } = DatabaseConfig.Sqlite;

    public PostgresConfig? Postgres { get; init; }
}

/// <summary>
/// Vyber databazy simulatora mimo nej samej - subor <c>database.json</c> v priecinku nastaveni. Bez suboru SQLite
/// v <c>tablesimulator.db</c>. Heslo k PostgreSQL je v subore zasifrovane DPAPI pre pocitac (ako kluce ochrany dat);
/// rucne napisane citatelne heslo server pri starte zasifruje a subor prepise. Zmena sa prejavi po restarte servera.
/// </summary>
public static class DatabaseConfig
{
    public const string Sqlite = "sqlite";
    public const string Postgres = "postgres";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Subor s vyberom databazy.</summary>
    public static string FileOf(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return Path.Combine(paths.DataFolder, "database.json");
    }

    /// <summary>Nacita vyber databazy (bez suboru SQLite); citatelne heslo zasifruje a subor prepise.</summary>
    /// <exception cref="InvalidOperationException">subor sa neda precitat alebo ma neznameho poskytovatela</exception>
    public static DatabaseConfigFile Load(AppPaths paths, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var file = FileOf(paths);
        if (!File.Exists(file))
            return new DatabaseConfigFile();
        DatabaseConfigFile config;
        try
        {
            config = JsonSerializer.Deserialize<DatabaseConfigFile>(File.ReadAllText(file), Json) ?? new DatabaseConfigFile();
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException($"{file}: {e.Message}", e);
        }

        if (config.Provider is not (Sqlite or Postgres))
            throw new InvalidOperationException($"{file}: unknown provider '{config.Provider}' (sqlite or postgres).");
        if (config.Postgres is { Password: { Length: > 0 } } && OperatingSystem.IsWindows())
        {
            Save(paths, config);
            config = Load(paths, logger);
            DatabaseLog.PasswordProtected(logger, file);
        }

        return config;
    }

    /// <summary>Zapise vyber databazy; heslo vo Windows zasifruje.</summary>
    public static void Save(AppPaths paths, DatabaseConfigFile config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.Postgres is { Password: { Length: > 0 } password } postgres && OperatingSystem.IsWindows())
            config = config with { Postgres = postgres with { Password = null, ProtectedPassword = Protect(password) } };
        Directory.CreateDirectory(paths.DataFolder);
        File.WriteAllText(FileOf(paths), JsonSerializer.Serialize(config, Json));
    }

    /// <summary>Pripojenie k databaze podla vyberu.</summary>
    public static DatabaseConnection ToConnection(AppPaths paths, DatabaseConfigFile config)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(config);
        if (config.Provider != Postgres)
            return DatabaseConnection.Sqlite(paths.DatabaseFile);
        var postgres = config.Postgres ?? new PostgresConfig();
        return new DatabaseConnection(DatabaseProvider.Postgres, ConnectionString(postgres, PasswordOf(postgres)));
    }

    /// <summary>Pripojenie Npgsql z nastavenia a hesla (prazdne = bez hesla).</summary>
    public static string ConnectionString(PostgresConfig postgres, string? password)
    {
        ArgumentNullException.ThrowIfNull(postgres);
        var builder = new NpgsqlConnectionStringBuilder(postgres.Options)
        {
            Host = postgres.Host, Port = postgres.Port, Database = postgres.Database
        };
        if (postgres.Username.Length > 0)
            builder.Username = postgres.Username;
        if (!string.IsNullOrEmpty(password))
            builder.Password = password;
        return builder.ConnectionString;
    }

    /// <summary>Heslo z nastavenia: zasifrovane (len na tomto pocitaci) alebo citatelne; null = ziadne.</summary>
    public static string? PasswordOf(PostgresConfig postgres)
    {
        ArgumentNullException.ThrowIfNull(postgres);
        if (postgres.Password is { Length: > 0 } plain)
            return plain;
        return postgres.ProtectedPassword is { Length: > 0 } protectedPassword && OperatingSystem.IsWindows() ? Unprotect(protectedPassword) : null;
    }

    [SupportedOSPlatform("windows")]
    private static string Protect(string password)
    {
        var encrypted = new DpapiXmlEncryptor(protectToLocalMachine: true, NullLoggerFactory.Instance).Encrypt(new XElement("password", password));
        return encrypted.EncryptedElement.ToString(SaveOptions.DisableFormatting);
    }

    [SupportedOSPlatform("windows")]
    private static string Unprotect(string value)
    {
        try
        {
            return new DpapiXmlDecryptor(null).Decrypt(XElement.Parse(value)).Value;
        }
        catch (Exception e) when (e is System.Security.Cryptography.CryptographicException or System.Xml.XmlException)
        {
            throw new InvalidOperationException("The PostgreSQL password in database.json cannot be decrypted on this computer - enter it again.", e);
        }
    }
}

internal static partial class DatabaseLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Heslo k PostgreSQL v {File} je teraz zasifrovane pre tento pocitac")]
    public static partial void PasswordProtected(ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Databaza: {Database}")]
    public static partial void Using(ILogger logger, string database);

    [LoggerMessage(Level = LogLevel.Information, Message = "Nova databaza PostgreSQL prevzala nastavenie servera zo SQLite {File}")]
    public static partial void SettingsFromSqlite(ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vybrana databaza: {Database} - pouzije sa po restarte servera")]
    public static partial void Selected(ILogger logger, string database);
}
