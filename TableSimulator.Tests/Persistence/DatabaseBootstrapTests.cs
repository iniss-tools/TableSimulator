using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TableSimulator.Application.Settings;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using TableSimulator.Infrastructure.Persistence;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Settings;
using TableSimulator.Persistence.Simulation;

namespace TableSimulator.Tests.Persistence;

/// <summary>
/// Databaza pri starte: prevod starych suborov settings.json a config.json (raz, poskodeny sa odlozi), predvolene
/// udaje novej databazy, ukladanie liniek a tabul s klucmi enumov z JSON.
/// </summary>
[TestClass]
public sealed class DatabaseBootstrapTests
{
    private readonly List<string> _folders = [];

    private AppPaths Paths()
    {
        var folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        _folders.Add(folder);
        return new AppPaths(folder, Path.Combine(folder, "wwwroot"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var folder in _folders)
            Directory.Delete(folder, true);
    }

    private static readonly SimulatorConfig Config = new()
    {
        Lines =
        [
            new LineConfig(3, 4, "TCP://0.0.0.0:47003") { Name = "Odchody", Mode = LineMode.Monitor, AutoLearn = false, Manufacturer = "ELEN16", Mirror = "COM10:9600,n,8,1" },
            new LineConfig(4, 5, "TCP://0.0.0.0:47004")
        ],
        Boards =
        [
            new BoardConfig(3, 5, "ELEN16")
            {
                Name = "ODCH1", Design = "elen-led", SecondAddress = 6, Fault = new BoardFault(FaultKind.NoReply),
                Profile = new BoardProfile { Width = 160, Rows = 4, FontMap = new Dictionary<string, int> { ["face0"] = 3 }, Header = new BoardHeader { Height = 8 } }
            },
            new BoardConfig(4, 12, "LCD1") { Name = "NAST1" }
        ]
    };

    private static async Task<SimulatorConfig> LoadAsync(AppPaths paths)
    {
        await using var db = ServerDatabase.Open(paths.DatabaseFile);
        return await new SimulatorConfigRepository(db).LoadAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task StareSubory_PrevedieRazAPremenuje()
    {
        var paths = Paths();
        await File.WriteAllTextAsync(paths.LegacySettingsFile, """{"httpPort":5999,"requireLogin":true,"monitorFolder":"C:\\Monitor","virtualFolders":["C:\\Logy"]}""");
        await File.WriteAllTextAsync(paths.LegacyConfigFile, SimulatorConfigJson.Serialize(Config));

        var settings = await DatabaseBootstrap.PrepareAsync(paths, NullLogger.Instance);

        Assert.AreEqual(5999, settings.HttpPort);
        Assert.IsTrue(settings.RequireLogin);
        Assert.AreEqual(@"C:\Monitor", settings.MonitorFolder);
        CollectionAssert.AreEqual(new[] { @"C:\Logy" }, settings.VirtualFolders.ToArray());
        Assert.AreEqual(SimulatorConfigJson.Serialize(Config), SimulatorConfigJson.Serialize(await LoadAsync(paths)));
        Assert.IsTrue(File.Exists(paths.LegacySettingsFile + DatabaseBootstrap.ImportedSuffix));
        Assert.IsFalse(File.Exists(paths.LegacyConfigFile));

        // druhy start - z databazy, novy subor sa uz nepouzije
        await File.WriteAllTextAsync(paths.LegacySettingsFile, """{"httpPort":6000}""");
        Assert.AreEqual(5999, (await DatabaseBootstrap.PrepareAsync(paths, NullLogger.Instance)).HttpPort);
        Assert.IsTrue(File.Exists(paths.LegacySettingsFile));
    }

    [TestMethod]
    public async Task NovaDatabaza_PredvoleneNastavenieAJednaLinka()
    {
        var paths = Paths();

        var settings = await DatabaseBootstrap.PrepareAsync(paths, NullLogger.Instance);

        Assert.AreEqual(new AppSettings().HttpPort, settings.HttpPort);
        Assert.AreEqual(SimulatorConfigJson.Serialize(DatabaseBootstrap.DefaultConfig()), SimulatorConfigJson.Serialize(await LoadAsync(paths)));
    }

    [TestMethod]
    public async Task PoskodenySubor_OdlozenyAPredvoleneUdaje()
    {
        var paths = Paths();
        await File.WriteAllTextAsync(paths.LegacySettingsFile, "{zle");
        await File.WriteAllTextAsync(paths.LegacyConfigFile, "[]");

        var settings = await DatabaseBootstrap.PrepareAsync(paths, NullLogger.Instance);

        Assert.AreEqual(new AppSettings().HttpPort, settings.HttpPort);
        Assert.IsTrue(File.Exists(paths.LegacySettingsFile + ".bad"));
        Assert.IsTrue(File.Exists(paths.LegacyConfigFile + ".bad"));
        Assert.AreEqual(1, (await LoadAsync(paths)).Lines.Single().Number);
    }

    [TestMethod]
    public async Task Ulozenie_NahradiVsetkoAEnumyAkoKluceJson()
    {
        var paths = Paths();
        await DatabaseBootstrap.PrepareAsync(paths, NullLogger.Instance);
        await using var db = ServerDatabase.Open(paths.DatabaseFile);
        var repository = new SimulatorConfigRepository(db);

        await repository.SaveAsync(Config, CancellationToken.None);
        var modes = await db.Database.SqlQueryRaw<string>("SELECT Mode AS Value FROM Lines ORDER BY Number").ToListAsync();
        var faults = await db.Database.SqlQueryRaw<string>("SELECT FaultKind AS Value FROM Boards ORDER BY Line").ToListAsync();
        await repository.SaveAsync(new SimulatorConfig { Lines = [Config.Lines[1]], Boards = [Config.Boards[1]] }, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "monitor", "display" }, modes);
        CollectionAssert.AreEqual(new[] { "noReply", "none" }, faults);
        var saved = await repository.LoadAsync(CancellationToken.None);
        Assert.AreEqual(4, saved.Lines.Single().Number);
        Assert.AreEqual("NAST1", saved.Boards.Single().Name);
    }

    [TestMethod]
    public async Task Nastavenie_UlozeneVsetkyHodnoty()
    {
        var paths = Paths();
        await DatabaseBootstrap.PrepareAsync(paths, NullLogger.Instance);
        var settings = new AppSettings
        {
            HttpPort = 5480, AllowNetwork = true, RequireLogin = true, FontsFolder = "F", DesignsFolder = "D", ConnectHost = "10.0.0.5",
            MonitorFolder = "M", VirtualFolders = ["A", "B"], HttpsPort = 5481, HttpsRedirect = false, HttpsCertificate = "c.pfx", HttpsKey = "k.pem",
            HttpsPassword = "sifrovane"
        };

        await using (var db = ServerDatabase.Open(paths.DatabaseFile))
            new SettingsRepository(db).Save(settings);
        await using var read = ServerDatabase.Open(paths.DatabaseFile);
        var loaded = new SettingsRepository(read).Load()!;

        Assert.AreEqual(settings with { VirtualFolders = loaded.VirtualFolders }, loaded);
        CollectionAssert.AreEqual(new[] { "A", "B" }, loaded.VirtualFolders.ToArray());
    }
}
