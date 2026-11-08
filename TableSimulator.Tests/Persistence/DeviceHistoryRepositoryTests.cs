using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using TableSimulator.Application.Devices;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.AkSignal;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.AkSignal;
using TableSimulator.Core.Simulation;
using TableSimulator.Infrastructure.Persistence;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Devices;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Tests.Persistence;

/// <summary>
/// Historia ustredni AK Signal v SQLite: zapis, dotaz so stavom na zaciatku obdobia, hlasenia v obdobi, mazanie starej
/// historie a dotaz s logom svetiel servera INISS2 priradenym ustredniam podla nazvu svetla.
/// </summary>
[TestClass]
public sealed class DeviceHistoryRepositoryTests
{
    private static readonly DateTimeOffset T0 = new(2030, 1, 7, 10, 0, 0, TimeSpan.Zero);

    private string _folder = null!;

    [TestInitialize]
    public void Init()
    {
        _folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, true);
    }

    private async Task<string> DatabaseAsync()
    {
        var paths = new AppPaths(_folder, Path.Combine(_folder, "wwwroot"));
        await DatabaseBootstrap.PrepareAsync(paths, DatabaseConnection.Sqlite(paths.DatabaseFile), NullLogger.Instance);
        return paths.DatabaseFile;
    }

    private static DateTimeOffset At(int minutes) => T0.AddMinutes(minutes);

    private static long Ms(int minutes) => At(minutes).ToUnixTimeMilliseconds();

    private static DeviceHistorySample Sample() => new(
        [
            new LightSample(6, 3, At(-60), LightFlags.Dark),
            new LightSample(6, 3, At(-30), LightFlags.Dark | LightFlags.Auto | LightFlags.On),
            new LightSample(6, 3, At(10), LightFlags.Dark),
            new LightSample(7, 0, At(-5), LightFlags.None)
        ],
        [
            new AnnouncementSample(7, 0, new Announcement(1, 1, ["CZ/a", "CZ/b"], At(-1)) { Duration = TimeSpan.FromSeconds(90) }.End(At(1), AnnouncementResult.Cancelled)),
            new AnnouncementSample(6, 3, new Announcement(2, 0, [], At(-90)) { Value = 1 }.End(At(-80), AnnouncementResult.Completed))
        ]);

    [TestMethod]
    public async Task Historia_DotazSoStavomNaZaciatku()
    {
        var file = await DatabaseAsync();
        await using (var db = ServerDatabase.Open(file))
            await new DeviceHistoryRepository(db).AddAsync(Sample(), CancellationToken.None);

        await using var read = ServerDatabase.Open(file);
        var data = await new DeviceHistoryRepository(read).QueryAsync(Ms(0), Ms(60), null, null, CancellationToken.None);

        // stav pred obdobim (posledny pri kazdej ustredni) a zmeny v nom
        CollectionAssert.AreEquivalent(new[] { Ms(-30), Ms(10), Ms(-5) }, data.Lights.Select(l => l.TimeMs).ToArray());
        var cancelled = data.Announcements.Single();
        Assert.AreEqual("cancelled", cancelled.Result);
        Assert.AreEqual("CZ/a*CZ/b", cancelled.Sounds);

        var one = await new DeviceHistoryRepository(read).QueryAsync(Ms(0), Ms(60), 6, 3, CancellationToken.None);
        CollectionAssert.AreEqual(new[] { Ms(-30), Ms(10) }, one.Lights.Select(l => l.TimeMs).Order().ToArray());
        Assert.IsEmpty(one.Announcements);
    }

    [TestMethod]
    public async Task Historia_MazanieNechaPoslednyStav()
    {
        var file = await DatabaseAsync();
        await using var db = ServerDatabase.Open(file);
        var repository = new DeviceHistoryRepository(db);
        await repository.AddAsync(Sample(), CancellationToken.None);

        Assert.AreEqual(2, await repository.DeleteOlderThanAsync(Ms(-20), CancellationToken.None), "najstarsi stav a stare hlasenie");

        var data = await repository.QueryAsync(Ms(-120), Ms(60), null, null, CancellationToken.None);
        CollectionAssert.AreEquivalent(new[] { Ms(-30), Ms(10), Ms(-5) }, data.Lights.Select(l => l.TimeMs).ToArray());
    }

    [TestMethod]
    public async Task Dotaz_LogServeraPodlaNazvuSvetla()
    {
        var file = await DatabaseAsync();
        await using (var db = ServerDatabase.Open(file))
            await new DeviceHistoryRepository(db).AddAsync(Sample(), CancellationToken.None);

        // instalacia servera INISS2 s logom svetiel
        var server = Path.Combine(_folder, "INISS2", "Server");
        Directory.CreateDirectory(Path.Combine(server, "Data"));
        Directory.CreateDirectory(Path.Combine(server, "LOG"));
        File.WriteAllText(Path.Combine(server, "Data", "Config.xml"), "<INISSCFG />");
        File.WriteAllBytes(Path.Combine(server, "IniSrv.exe"), []);
        var local = At(5).ToLocalTime();
        File.WriteAllText(ToolsCore.Iniss2.Devices.LightLogFile.PathFor(Path.Combine(server, "LOG"), DateOnly.FromDateTime(local.DateTime)),
            $"""<L d="{local:yyyy-MM-dd}" c="{local:HH:mm:ss}{local:zzz}" s="Lipovec" n="Nástupište 1" u="ChangeForTrain" r="AUTO" t="true" off="{At(12):O}" dcv="6412" />"""
            + "\r\n" + $"""<L d="{local:yyyy-MM-dd}" c="{local:HH:mm:ss}{local:zzz}" s="Lipovec" n="Iné" u="ChangeState" r="OFF" />""", Encodings.Win1250);

        var simulator = new Simulator();
        simulator.SetLine(new LineConfig(6, 131, "TCP://127.0.0.1:47006") { AutoLearn = false, Manufacturer = AkSignalProtocol.Manufacturer });
        simulator.SetBoard(new BoardConfig(6, 3, AkSignalProtocol.Manufacturer)
        {
            Profile = new BoardProfile { AkSignal = new AkSignalProfile { Lights = [new LightCircuit("Nástupište 1", 5400010)] } }
        });

        await using var read = ServerDatabase.Open(file);
        var handler = new GetDeviceHistoryQueryHandler(new DeviceHistoryRepository(read), simulator,
            new Settings(new AppSettings { InissProgram = Path.Combine(server, "IniSrv.exe") }));
        var history = (await handler.Handle(new GetDeviceHistoryQuery(At(0), At(60), null), CancellationToken.None)).Value;

        Assert.HasCount(2, history.Server);
        var train = history.Server.Single(e => e.Event == ServerLightEvent.Train);
        Assert.AreEqual("6-3", train.BoardId);
        Assert.AreEqual("6412", train.Train);
        Assert.AreEqual(At(12), train.TurnOff);
        Assert.IsNull(history.Server.Single(e => e.Event == ServerLightEvent.State).BoardId, "svetlo, ktore ziadna ustredna nema");
        Assert.IsTrue(history.Lights.Single(l => l.At == At(10)).Dark);

        var other = (await handler.Handle(new GetDeviceHistoryQuery(At(0), At(60), "7-0"), CancellationToken.None)).Value;
        Assert.IsEmpty(other.Server, "log patri inej ustredni");
    }

    private sealed class Settings(AppSettings settings) : ISettingsStore
    {
        public AppSettings Current { get; private set; } = settings;

        public void Save(AppSettings settings) => Current = settings;
    }
}
