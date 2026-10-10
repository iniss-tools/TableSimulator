using Microsoft.Extensions.Logging;
using Npgsql;
using TableSimulator.Application.Database;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Database;
using TableSimulator.Persistence;

namespace TableSimulator.Infrastructure.Persistence;

/// <summary>
/// Vyber databazy v <c>database.json</c> z webu: zapis (heslo zasifrovane pre pocitac), porovnanie s pouzivanou
/// databazou a skuska pripojenia k PostgreSQL. Server pouziva vybranu databazu az po restarte.
/// </summary>
public sealed class DatabaseSettings(AppPaths paths, DatabaseConnection active, ILogger<DatabaseSettings> logger) : IDatabaseSettings
{
    private const int TestTimeoutSeconds = 5;

    public DatabaseSettingsDto Get()
    {
        string? error = null;
        DatabaseConfigFile config;
        try
        {
            config = DatabaseConfig.Load(paths, logger);
        }
        catch (InvalidOperationException e)
        {
            config = new DatabaseConfigFile();
            error = e.Message;
        }

        bool restart;
        try
        {
            restart = DatabaseConfig.ToConnection(paths, config) != active;
        }
        catch (InvalidOperationException e)
        {
            restart = true;
            error ??= e.Message;
        }

        var postgres = config.Postgres ?? new PostgresConfig();
        return new DatabaseSettingsDto(
            active.Provider == DatabaseProvider.Postgres ? DatabaseKind.Postgres : DatabaseKind.Sqlite,
            active.Description,
            config.Provider == DatabaseConfig.Postgres ? DatabaseKind.Postgres : DatabaseKind.Sqlite,
            new PostgresSettingsDto(postgres.Host, postgres.Port, postgres.Database, postgres.Username,
                !string.IsNullOrEmpty(postgres.Password) || !string.IsNullOrEmpty(postgres.ProtectedPassword), postgres.Options),
            restart,
            error);
    }

    public void Save(UpdateDatabaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var stored = Stored();
        var r = request.Postgres;
        var postgres = new PostgresConfig
        {
            Host = r.Host.Trim(), Port = r.Port, Database = r.Database.Trim(), Username = r.Username.Trim(), Options = r.Options.Trim(),
            // null = nechat ulozene, prazdne = zmazat
            Password = r.Password is { Length: > 0 } ? r.Password : r.Password is null ? stored.Password : null,
            ProtectedPassword = r.Password is null ? stored.ProtectedPassword : null
        };
        var config = new DatabaseConfigFile
        {
            Provider = request.Provider == DatabaseKind.Postgres ? DatabaseConfig.Postgres : DatabaseConfig.Sqlite, Postgres = postgres
        };
        DatabaseConfig.Save(paths, config);
        DatabaseLog.Selected(logger, config.Provider == DatabaseConfig.Postgres
            ? new DatabaseConnection(DatabaseProvider.Postgres, DatabaseConfig.ConnectionString(postgres, null)).Description
            : paths.DatabaseFile);
    }

    public async Task<DatabaseTestResultDto> TestAsync(PostgresSettingsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        NpgsqlConnectionStringBuilder builder;
        try
        {
            var postgres = new PostgresConfig
            {
                Host = request.Host.Trim(), Port = request.Port, Database = request.Database.Trim(), Username = request.Username.Trim(),
                Options = request.Options.Trim()
            };
            var password = request.Password ?? DatabaseConfig.PasswordOf(Stored());
            builder = new NpgsqlConnectionStringBuilder(DatabaseConfig.ConnectionString(postgres, password))
            {
                Timeout = TestTimeoutSeconds, CommandTimeout = TestTimeoutSeconds, Pooling = false
            };
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException)
        {
            return Failed(e.Message);
        }

        try
        {
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var initialized = await ScalarAsync(connection, "SELECT to_regclass('\"__EFMigrationsHistory\"') IS NOT NULL", cancellationToken)
                .ConfigureAwait(false);
            // tabulky simulatora zalozi server v aktualnej scheme
            var canCreate = await ScalarAsync(connection, "SELECT COALESCE(has_schema_privilege(current_schema(), 'CREATE'), false)", cancellationToken)
                .ConfigureAwait(false);
            return new DatabaseTestResultDto(true, null, connection.ServerVersion, true, canCreate, initialized);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            // prihlasenie preslo, databaza chyba - server ju pri starte zalozi, ak na to pouzivatel smie
            return await MissingDatabaseAsync(builder, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is NpgsqlException or TimeoutException or ArgumentException)
        {
            return Failed(e.Message);
        }
    }

    /// <summary>Skuska pri chybajucej databaze: verzia servera a pravo CREATEDB cez spravcovsku databazu (ako ju zaklada Npgsql).</summary>
    private static async Task<DatabaseTestResultDto> MissingDatabaseAsync(NpgsqlConnectionStringBuilder builder, CancellationToken cancellationToken)
    {
        foreach (var admin in new[] { "postgres", "template1" })
        {
            try
            {
                await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(builder.ConnectionString) { Database = admin }.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var canCreate = await ScalarAsync(connection, "SELECT COALESCE((SELECT rolcreatedb OR rolsuper FROM pg_roles WHERE rolname = current_user), false)",
                    cancellationToken).ConfigureAwait(false);
                return new DatabaseTestResultDto(true, null, connection.ServerVersion, false, canCreate, false);
            }
            catch (NpgsqlException)
            {
                // dalsia spravcovska databaza
            }
        }

        return new DatabaseTestResultDto(true, null, null, false, false, false);
    }

    private static async Task<bool> ScalarAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true;
    }

    private static DatabaseTestResultDto Failed(string error) => new(false, error, null, false, false, false);

    /// <summary>Ulozene pripojenie k PostgreSQL (aj ked subor chyba alebo sa neda precitat).</summary>
    private PostgresConfig Stored()
    {
        try
        {
            return DatabaseConfig.Load(paths, logger).Postgres ?? new PostgresConfig();
        }
        catch (InvalidOperationException)
        {
            return new PostgresConfig();
        }
    }
}
