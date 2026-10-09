using TableSimulator.Application.Settings;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Station;
using ToolsCore.Iniss.Grafikon;
using ToolsCore.Iniss.Tables;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Nacitanie linek a tabul instalacie INISSu z nastavenia simulatora (bez GVDEditora): linky z .INI, tabule z grafikonu,
/// port linky z presmerovaneho TablePort, tabule bez linky a XML pre virtualne tabule.
/// </summary>
[TestClass]
public sealed class InissStationsTests
{
    private DirectoryInfo _dir = null!;
    private string _program = null!;

    [TestInitialize]
    public void Init()
    {
        _dir = Directory.CreateTempSubdirectory("tsiniss");
        // jedinecne meno programu - vetva registra s tymto menom na pocitaci nie je, cita sa len .INI
        var name = "INISSTEST" + Guid.NewGuid().ToString("N")[..8];
        _program = Path.Combine(_dir.FullName, name + ".exe");
        File.WriteAllBytes(_program, []);
        File.WriteAllText(Path.Combine(_dir.FullName, name + ".INI"),
            "[Driver]\r\nTableClass=4\r\nTablePort=1=TCP://127.0.0.1:47201\r\n[Driver0]\r\nTableClass=4\r\nTablePort=COM2\r\n[PathNames]\r\nLogPath=Logy\r\n",
            Encodings.Win1250);

        var data = Directory.CreateDirectory(Path.Combine(_dir.FullName, GvdFileConsts.DirData));
        File.WriteAllText(Path.Combine(data.FullName, GvdFileConsts.FileDirlist), "Stanica\r\n", Encodings.Win1250);
        var grafikon = Directory.CreateDirectory(Path.Combine(data.FullName, "Stanica"));
        InfoGvdFile.Write(grafikon.FullName, new GVDInfo { ThisStation = new Station("1234", "Testovacia") });

        var catalog = new TableCatalog
        {
            Key = "Odch", Name = "Odchodova", Comment = "", Manufacturer = TableManufacturer.Elen, MaxRecCount = 1, Items = [],
            Segments = [new TableSegment { Height = 10, Width = 128, Size = 15 }], ViewTypeTabs = []
        };
        TablesFile.Write(grafikon.FullName, [], [catalog],
            [Physical("Nastupiste", 3, 1, catalog), Physical("Hala", 4, 0, catalog, "TAB1"), Physical("Bez linky", 5, 7, catalog)], []);
    }

    [TestCleanup]
    public void Cleanup() => _dir.Delete(true);

    private static TablePhysical Physical(string key, int id, int port, TableCatalog catalog, string xml = "") => new()
    {
        Key = key, Name = key, ID = id, CommunicationPort = port, Rem = "", SaveXML = xml, ReverseArrows = "", Comment = "", TableCatalog = catalog
    };

    [TestMethod]
    public void Nacitanie_LinkyTabuleAPortZPresmerovania()
    {
        var result = InissStations.Read(new AppSettings { InissProgram = _program });

        Assert.IsTrue(result.IsSuccess, result.Error.Description);
        var station = result.Value;
        Assert.HasCount(2, station.Lines);
        var first = station.Lines[0];
        Assert.AreEqual(1, first.Number);
        // presmerovany INISS posiela na port simulatora
        Assert.AreEqual(47201, first.SimulatorPort);
        // tabula s linkou 1 a tabula s automatickym priradenim (prva linka s rodinou ELEN)
        CollectionAssert.AreEquivalent(new[] { "Nastupiste", "Hala" }, first.Tables.Select(t => t.Name).ToArray());
        Assert.IsTrue(first.Tables.All(t => t.Shown && t.Manufacturer == "ELEN"));
        Assert.IsTrue(first.Tables.Single(t => t.Name == "Hala").Automatic);

        // COM2 = linka 2, v simulatore predvoleny port
        Assert.AreEqual(2, station.Lines[1].Number);
        Assert.AreEqual(47002, station.Lines[1].SimulatorPort);
        Assert.IsTrue(station.Lines[1].Problems.Any(p => p.Code == InissProblemCode.NoTables));

        var unserved = station.Unserved.Single();
        Assert.AreEqual("Bez linky", unserved.Name);
        Assert.AreEqual(InissProblemCode.NoDriver, unserved.Problem!.Code);
        CollectionAssert.AreEqual(new[] { "7" }, unserved.Problem.Args.ToArray());

        Assert.AreEqual(Path.Combine(_dir.FullName, "Logy"), station.LogFolder);
        Assert.IsFalse(station.LogFolderExists);
        Assert.AreEqual("TAB1", station.XmlTables.Single().Source);
        CollectionAssert.AreEqual(new[] { "Stanica" }, station.Grafikons.ToArray());
    }

    [TestMethod]
    public void Poziadavka_AktivneLinkyAIchTabule()
    {
        var request = InissStations.ToRequest(InissStations.Read(new AppSettings { InissProgram = _program }).Value);

        CollectionAssert.AreEqual(new[] { (1, 47201), (2, 47002) }, request.Lines.Select(l => (l.Number, l.Port)).ToArray());
        CollectionAssert.AreEquivalent(new[] { (1, 3), (1, 4) }, request.Boards.Select(b => (b.Line, b.Address)).ToArray());
        Assert.AreEqual(Path.Combine(_dir.FullName, "Logy"), request.LogFolder);
        Assert.HasCount(1, request.XmlTables);
    }

    [TestMethod]
    public void RiadiaciInissSoVzdialenym_LinkaTriedy128SoVsetkymiTabulami()
    {
        File.WriteAllText(Path.ChangeExtension(_program, ".INI"),
            "[Driver]\r\nTableClass=4\r\nTablePort=1=TCP://127.0.0.1:47201\r\n[Driver1]\r\nTableClass=128\r\nTablePort=4=tcp://192.168.10.20:8090\r\n",
            Encodings.Win1250);
        var grafikon = Path.Combine(_dir.FullName, GvdFileConsts.DirData, "Stanica");
        var elen = new TableCatalog
        {
            Key = "Odch", Name = "Odchodova", Comment = "", Manufacturer = TableManufacturer.Elen16, MaxRecCount = 1, Items = [],
            Segments = [new TableSegment { Height = 10, Width = 128, Size = 15 }], ViewTypeTabs = []
        };
        var lcd = new TableCatalog
        {
            Key = "Nast", Name = "Nastupistna", Comment = "", Manufacturer = TableManufacturer.Lcd1, MaxRecCount = 1, Items = [],
            Segments = [new TableSegment { Height = 10, Width = 128, Size = 15 }], ViewTypeTabs = []
        };
        TablesFile.Write(grafikon, [], [elen, lcd],
            [Physical("Miestna", 3, 1, elen), Physical("Hala vzdialena", 12, 4, elen), Physical("Nastupiste vzdialene", 20, 4, lcd)], []);

        var station = InissStations.Read(new AppSettings { InissProgram = _program }).Value;
        var remote = station.Lines.Single(l => l.Number == 4);
        Assert.AreEqual(128, remote.TableClass);
        Assert.IsTrue(remote.Supported);
        Assert.IsTrue(remote.Tables.All(t => t.Shown));

        var request = InissStations.ToRequest(station);
        Assert.AreEqual(8090, request.Lines.Single(l => l.Number == 4).Port);
        CollectionAssert.AreEquivalent(new[] { (4, 12, "ELEN16"), (4, 20, "LCD1") },
            request.Boards.Where(b => b.Line == 4).Select(b => (b.Line, b.Address, b.Manufacturer)).ToArray());
    }

    [TestMethod]
    [DataRow("", "iniss.notConfigured")]
    [DataRow("chyba.exe", "iniss.programNotFound")]
    public void Chyby_NenastavenyAleboChybajuciProgram(string program, string code)
    {
        var path = program.Length == 0 ? "" : Path.Combine(_dir.FullName, program);
        Assert.AreEqual(code, InissStations.Read(new AppSettings { InissProgram = path }).Error.Code);
    }

    [TestMethod]
    public void Chyby_BezPriecinkaData()
    {
        Directory.Delete(Path.Combine(_dir.FullName, GvdFileConsts.DirData), true);
        Assert.AreEqual("iniss.dataNotFound", InissStations.Read(new AppSettings { InissProgram = _program }).Error.Code);
    }

    [TestMethod]
    public void ZosilnovaceElsvo_ZoZvukovychOkruhovPodlaCislaLinky()
    {
        File.WriteAllLines(Path.ChangeExtension(_program, ".INI"),
            ["[Driver]", "TableClass=4", "TablePort=1=TCP://127.0.0.1:47201", "[Driver0]", "TableClass=129", "TablePort=COM3"], Encodings.Win1250);
        File.WriteAllLines(Path.Combine(_dir.FullName, GvdFileConsts.DirData, GvdFileConsts.FileAudio),
        [
            "1234,Nástupište 1,N1,F1,,,,E3,5", "1234,Hala,H,F2,,,,E3,", "1234,Ústredňa,U,F3,,,,12,1", "1234,Iná linka,I,F4,,,,E1,2",
            "1234,Bez linky,B,F5,,,,E9,1"
        ], Encodings.Win1250);

        var station = InissStations.Read(new AppSettings { InissProgram = _program }).Value;

        var elsvo = station.Lines.Single(l => l.Number == 3);
        Assert.IsTrue(elsvo.Supported);
        CollectionAssert.AreEqual(new[] { (5, "Nástupište 1", true), (0, "Hala", true) }, elsvo.Amplifiers.Select(a => (a.Address, a.Name, a.Shown)).ToArray());
        // jednotka na linke ELEN sa nezobrazi, jednotka bez linky je medzi neobsluzenymi
        Assert.IsFalse(station.Lines.Single(l => l.Number == 1).Amplifiers.Single().Shown);
        Assert.AreEqual((9, "Bez linky"), (station.UnservedAmplifiers.Single().Line, station.UnservedAmplifiers.Single().Name));

        var request = InissStations.ToRequest(station);
        CollectionAssert.AreEquivalent(new[] { (3, 5, "ELSVO"), (3, 0, "ELSVO"), (1, 2, "ELSVO"), (1, 3, "ELEN"), (1, 4, "ELEN") },
            request.Boards.Select(b => (b.Line, b.Address, b.Manufacturer)).ToArray());
    }
}
