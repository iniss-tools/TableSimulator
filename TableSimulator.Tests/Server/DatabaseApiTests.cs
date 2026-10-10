using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using TableSimulator.Contracts.Database;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Settings;
using TableSimulator.Contracts.State;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Vyber databazy z webu (<c>database.json</c>, heslo zasifrovane pre pocitac) a server nad PostgreSQL. Testy so
/// serverom PostgreSQL bezia len s pripojenim v <c>TABLESIMULATOR_TEST_POSTGRES</c> (ako <see cref="Persistence.PostgresTests" />).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DatabaseApiTests
{
    private const string Variable = "TABLESIMULATOR_TEST_POSTGRES";

    private static PostgresSettingsRequest Postgres(string? password, string options = "") =>
        new() { Host = "db.example", Port = 5433, Database = "simulator", Username = "ts", Password = password, Options = options };

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static async Task<DatabaseSettingsDto> PutAsync(HttpClient client, UpdateDatabaseRequest request)
    {
        var response = await client.PutAsJsonAsync("/api/database", request);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<DatabaseSettingsDto>())!;
    }

    [TestMethod]
    public async Task VyberDatabazy_UlozenieAHeslo()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        var file = Path.Combine(server.DataFolder, "database.json");

        var current = (await client.GetFromJsonAsync<DatabaseSettingsDto>("/api/database"))!;
        Assert.AreEqual((DatabaseKind.Sqlite, DatabaseKind.Sqlite, false, false), (current.Active, current.Provider, current.RestartRequired, current.Postgres.PasswordSet));
        StringAssert.EndsWith(current.ActiveDescription, "tablesimulator.db");
        Assert.IsFalse(File.Exists(file));

        var saved = await PutAsync(client, new UpdateDatabaseRequest { Provider = DatabaseKind.Postgres, Postgres = Postgres("Tajne-heslo1") });
        Assert.AreEqual((DatabaseKind.Sqlite, DatabaseKind.Postgres, true, true), (saved.Active, saved.Provider, saved.RestartRequired, saved.Postgres.PasswordSet));
        Assert.AreEqual(("db.example", 5433, "simulator", "ts"), (saved.Postgres.Host, saved.Postgres.Port, saved.Postgres.Database, saved.Postgres.Username));
        var json = File.ReadAllText(file);
        Assert.DoesNotContain("Tajne-heslo1", json);
        StringAssert.Contains(json, "protectedPassword");

        // heslo null = nechat ulozene, prazdne = zmazat
        saved = await PutAsync(client, new UpdateDatabaseRequest { Provider = DatabaseKind.Postgres, Postgres = Postgres(null) with { Host = "db2.example" } });
        Assert.AreEqual(("db2.example", true), (saved.Postgres.Host, saved.Postgres.PasswordSet));
        saved = await PutAsync(client, new UpdateDatabaseRequest { Provider = DatabaseKind.Postgres, Postgres = Postgres("") });
        Assert.IsFalse(saved.Postgres.PasswordSet);

        // spat na SQLite - pripojenie k PostgreSQL ostane ulozene, restart netreba
        saved = await PutAsync(client, new UpdateDatabaseRequest { Provider = DatabaseKind.Sqlite, Postgres = Postgres(null) with { Host = "" } });
        Assert.AreEqual((DatabaseKind.Sqlite, false), (saved.Provider, saved.RestartRequired));

        var bad = await client.PutAsJsonAsync("/api/database", new UpdateDatabaseRequest { Provider = DatabaseKind.Postgres, Postgres = Postgres(null, "Password=x") });
        Assert.AreEqual("database.options", await CodeOf(bad));
        bad = await client.PutAsJsonAsync("/api/database", new UpdateDatabaseRequest { Provider = DatabaseKind.Postgres, Postgres = Postgres(null) with { Host = "" } });
        Assert.AreEqual("database.host", await CodeOf(bad));
        bad = await client.PostAsJsonAsync("/api/database/test", Postgres(null, "SSL Mode"));
        Assert.AreEqual("database.options", await CodeOf(bad));
    }

    [TestMethod]
    public async Task SkuskaPripojenia_NedostupnyServer()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        var response = await client.PostAsJsonAsync("/api/database/test", Postgres("x") with { Host = "127.0.0.1", Port = 1 });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var result = (await response.Content.ReadFromJsonAsync<DatabaseTestResultDto>())!;
        Assert.IsFalse(result.Ok);
        Assert.IsFalse(string.IsNullOrEmpty(result.Error));
    }

    [TestMethod]
    public async Task ServerNadPostgreSql_NastavenieZoSqliteAPrepnutieSpat()
    {
        var admin = Environment.GetEnvironmentVariable(Variable) ?? "";
        if (admin.Length == 0)
            Assert.Inconclusive($"{Variable} nie je nastavena - testy PostgreSQL sa preskocili.");
        var builder = new NpgsqlConnectionStringBuilder(admin);
        var name = "ts_test_" + Guid.NewGuid().ToString("N")[..12];
        var postgres = new PostgresSettingsRequest
        {
            Host = builder.Host!, Port = builder.Port, Database = name, Username = builder.Username ?? "", Password = builder.Password
        };
        string folder;
        try
        {
            await using (var server = SimulatorServer.Start("""{"connectHost":"10.0.0.5"}""", deleteFolder: false))
            {
                folder = server.DataFolder;
                using var client = server.Client();
                // skuska: databaza este nie je, pouzivatel ju smie zalozit
                var test = (await (await client.PostAsJsonAsync("/api/database/test", postgres)).Content.ReadFromJsonAsync<DatabaseTestResultDto>())!;
                Assert.AreEqual((true, false, true, false), (test.Ok, test.Exists, test.CanCreate, test.Initialized), test.Error);
                Assert.IsNotNull(test.Version);
                Assert.IsTrue((await PutAsync(client, new UpdateDatabaseRequest { Provider = DatabaseKind.Postgres, Postgres = postgres })).RestartRequired);
            }

            await using (var server = SimulatorServer.Open(folder, false))
            {
                using var client = server.Client();
                var current = (await client.GetFromJsonAsync<DatabaseSettingsDto>("/api/database"))!;
                Assert.AreEqual((DatabaseKind.Postgres, false), (current.Active, current.RestartRequired), current.Error);
                StringAssert.Contains(current.ActiveDescription, name);
                // prazdna databaza PostgreSQL prevzala nastavenie servera zo SQLite, linky nie
                var state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;
                Assert.AreEqual("10.0.0.5", state.Settings!.ConnectHost);
                StringAssert.Contains(state.Settings.DatabasePath, "PostgreSQL");
                Assert.IsEmpty(state.Lines);
                Assert.AreEqual(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/lines/5", new SetLineRequest
                {
                    TableClass = 4, Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N"), Name = "V PostgreSQL"
                })).StatusCode);
                var test = (await (await client.PostAsJsonAsync("/api/database/test", postgres with { Password = null })).Content
                    .ReadFromJsonAsync<DatabaseTestResultDto>())!;
                Assert.AreEqual((true, true, true), (test.Ok, test.Exists, test.Initialized), test.Error);
            }

            await using (var server = SimulatorServer.Open(folder, false))
            {
                using var client = server.Client();
                var state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;
                Assert.AreEqual("V PostgreSQL", state.Lines.Single().Name);
                await PutAsync(client, new UpdateDatabaseRequest { Provider = DatabaseKind.Sqlite, Postgres = postgres with { Password = null } });
            }

            // SQLite ostal bez zmeny
            await using (var server = SimulatorServer.Open(folder, true))
            {
                using var client = server.Client();
                Assert.AreEqual(DatabaseKind.Sqlite, (await client.GetFromJsonAsync<DatabaseSettingsDto>("/api/database"))!.Active);
                Assert.IsEmpty((await client.GetFromJsonAsync<StateDto>("/api/state"))!.Lines);
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(admin);
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
