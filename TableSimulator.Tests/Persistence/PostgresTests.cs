using Microsoft.EntityFrameworkCore;
using Npgsql;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Keys;
using TableSimulator.Application.Settings;
using TableSimulator.Application.Statistics;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Statistics;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Audit;
using TableSimulator.Persistence.Identity;
using TableSimulator.Persistence.Keys;
using TableSimulator.Persistence.Settings;
using TableSimulator.Persistence.Simulation;
using TableSimulator.Persistence.Statistics;

namespace TableSimulator.Tests.Persistence;

/// <summary>
/// Databaza simulatora v PostgreSQL: migracie, uloziska a prenos udajov SQLite → PostgreSQL → SQLite (zaloha a obnova
/// v spolocnom formate). Bezi len s pripojenim v premennej <c>TABLESIMULATOR_TEST_POSTGRES</c> (pouzivatel smie
/// zakladat databazy) - kazdy test si zalozi vlastnu databazu a na konci ju zmaze.
/// </summary>
[TestClass]
public sealed class PostgresTests
{
    private const string Variable = "TABLESIMULATOR_TEST_POSTGRES";

    private string _admin = null!;
    private string _name = null!;
    private DirectoryInfo _dir = null!;

    private DatabaseConnection Postgres => new(DatabaseProvider.Postgres,
        new NpgsqlConnectionStringBuilder(_admin) { Database = _name, Pooling = false }.ConnectionString);

    [TestInitialize]
    public async Task Init()
    {
        _admin = Environment.GetEnvironmentVariable(Variable) ?? "";
        if (_admin.Length == 0)
            Assert.Inconclusive($"{Variable} nie je nastavena - testy PostgreSQL sa preskocili.");
        _name = "ts_test_" + Guid.NewGuid().ToString("N")[..12];
        _dir = Directory.CreateTempSubdirectory("tspg");
        await using var connection = new NpgsqlConnection(_admin);
        await connection.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_name}\"", connection);
        await create.ExecuteNonQueryAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_admin.Length == 0)
            return;
        _dir.Delete(true);
        await using var connection = new NpgsqlConnection(_admin);
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>Nastavenie, linky s tabulami, audit, kluc, pouzivatel s rolou a statistika.</summary>
    private static async Task FillAsync(AppDbContext db)
    {
        new SettingsRepository(db).Save(new AppSettings { HttpPort = 5999, VirtualFolders = [@"C:\Logy"] });
        await new SimulatorConfigRepository(db).SaveAsync(new SimulatorConfig
        {
            Lines = [new LineConfig(3, 4, "TCP://0.0.0.0:47003") { Name = "Nástupištia" }],
            Boards = [new BoardConfig(3, 5, "ELEN16") { Name = "ODCH1" }]
        }, CancellationToken.None);
        var log = new AuditLog(db);
        await log.WriteAsync(new AuditEntry(0, new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc), "admin", "SetLine", "3", true, null, "{}", "::1"),
            CancellationToken.None);
        await log.WriteAsync(new AuditEntry(0, new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc), "admin", "SetBoard", "3-5", true, null, "{}", "::1"),
            CancellationToken.None);
        await new AccessKeyRepository(db).AddAsync(new AccessKey(Guid.NewGuid(), "Tablet", "device", "viewer", "abc", "hash1",
            new DateTime(2026, 10, 1, 6, 0, 0, DateTimeKind.Utc), "admin", null), CancellationToken.None);
        var role = new AppRole { Id = Guid.NewGuid(), Name = "panely", NormalizedName = "PANELY", DisplayName = "Panely", Permissions = ["simulator.view", "signage.manage"] };
        var user = new AppUser
        {
            Id = Guid.NewGuid(), UserName = "spravca", NormalizedUserName = "SPRAVCA", DisplayName = "Správca",
            CreatedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.FromHours(2))
        };
        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        await new TrafficStatisticsRepository(db).AddAsync(new TrafficSample(
            [new LineMinute(3, 1000, 5, 1, 0, 0, 0, 5, 0, 50, 5, 1, 0), new LineMinute(3, 1001, 2, 0, 0, 0, 0, 2, 0, 20, 2, 0, 0)],
            [new BoardMinute(3, 5, 1000, 5, 5, 0, 0, 12_000)],
            [new LineStatusChange(3, DateTimeOffset.FromUnixTimeMilliseconds(1000 * 60_000L), LineStatus.Connected, "127.0.0.1")]), CancellationToken.None);
        db.ChangeTracker.Clear();
    }

    private static async Task AssertDataAsync(AppDbContext db)
    {
        Assert.AreEqual(5999, new SettingsRepository(db).Load()!.HttpPort);
        var config = await new SimulatorConfigRepository(db).LoadAsync(CancellationToken.None);
        Assert.AreEqual("Nástupištia", config.Lines.Single().Name);
        Assert.AreEqual("ODCH1", config.Boards.Single().Name);
        var audit = await new AuditLog(db).ListAsync(null, "admin", null, 10, CancellationToken.None);
        CollectionAssert.AreEqual(new[] { "SetBoard", "SetLine" }, audit.Select(a => a.Action).ToArray());
        Assert.AreEqual(new DateTime(2026, 10, 10, 9, 0, 0), audit[0].Time);
        Assert.AreEqual("Tablet", (await new AccessKeyRepository(db).FindByHashAsync("hash1", CancellationToken.None))!.Name);
        var user = await db.Users.SingleAsync();
        Assert.AreEqual(new DateTimeOffset(2026, 10, 1, 6, 0, 0, TimeSpan.Zero), user.CreatedAt.ToUniversalTime());
        Assert.AreEqual(1, await db.UserRoles.CountAsync());
        // opravnenia roly: v SQLite JSON, v PostgreSQL pole text[]
        CollectionAssert.AreEqual(new[] { "simulator.view", "signage.manage" }, (await db.Roles.SingleAsync()).Permissions);
        var data = await new TrafficStatisticsRepository(db).QueryAsync(990, 1010, 10, null, CancellationToken.None);
        Assert.AreEqual(7, data.Lines.Single().Ok);
        Assert.AreEqual("connected", data.StatusChanges.Single().Status);
    }

    [TestMethod]
    public async Task Uloziska_MigracieAUdajeVPostgreSql()
    {
        await using var db = ServerDatabase.Open(Postgres);
        await ServerDatabase.MigrateAsync(db);
        await FillAsync(db);
        await AssertDataAsync(db);
    }

    [TestMethod]
    public async Task Prenos_SqliteDoPostgreSqlASpat()
    {
        var sqlite = Path.Combine(_dir.FullName, "zdroj.db");
        await using (var source = ServerDatabase.Open(sqlite))
        {
            await ServerDatabase.MigrateAsync(source);
            await FillAsync(source);
        }

        await using (var source = ServerDatabase.Open(sqlite))
        await using (var target = ServerDatabase.Open(Postgres))
        {
            await ServerDatabase.MigrateAsync(target);
            Assert.IsGreaterThan(10, await DatabaseCopier.CopyAsync(source, target));
        }

        await using (var db = ServerDatabase.Open(Postgres))
        {
            await AssertDataAsync(db);
            // postupnosti identity su za prenesenymi klucmi - novy zaznam auditu nenarazi na existujuci
            await new AuditLog(db).WriteAsync(new AuditEntry(0, DateTime.UtcNow, "system", "Novy", "-", true, null, "{}", null), CancellationToken.None);
            Assert.AreEqual(3, await db.AuditEntries.CountAsync());
        }

        var copy = Path.Combine(_dir.FullName, "kopia.db");
        await using (var source = ServerDatabase.Open(Postgres))
        await using (var target = ServerDatabase.Open(copy))
        {
            await ServerDatabase.MigrateAsync(target);
            await DatabaseCopier.CopyAsync(source, target);
        }

        await using (var db = ServerDatabase.Open(copy))
            await AssertDataAsync(db);
    }
}
