using Microsoft.Extensions.Logging.Abstractions;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Settings;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Nacitanie liniek a tabul servera INISS2 z fiktivnych dat a presmerovanie jeho liniek na simulator.
/// </summary>
[TestClass]
public sealed class Iniss2StationsTests
{
    private const string CommLines = """
        <?xml version="1.0" encoding="windows-1250"?>
        <CommLines>
          <line k="Tabule" typ="UDP_CLIENT" protocol="ITP" line="10.0.0.1:10001" />
          <line k="Zvuk" typ="TCP_CLIENT" protocol="AKSIGNAL" line="10.0.0.2:20" />
          <line k="Prazdna" typ="TCP_CLIENT" protocol="ELEN" line="10.0.0.3:30" />
          <line k="Vynechana" typ="TCP_CLIENT" protocol="ITP" line="10.0.0.4:40" omit="true" />
          <line k="Lcd" typ="SERIAL" protocol="LCD" line="COM1" />
        </CommLines>
        """;

    private const string Catalog = """
        <CATALOG>
          <t k="Web" n="Web" cls="INTERNET" />
          <t k="Nastupistna" n="Nastupistna" cls="LCD1">
            <div k="Cil" inf="CilNazev" align="LEFT"><pos c1="0" c2="17" /></div>
            <div k="Odjezd" inf="CasOdjezdu" align="CENTER"><pos c1="17" c2="22" /></div>
            <div k="Zpozdeni" inf="ZpozdeniOdjezd" align="RIGHT"><pos r="1" c1="17" c2="20" /></div>
            <div k="Napis" inf="Free" align="RIGHT"><pos r="1" c1="20" c2="23" /></div>
            <div k="Smer" inf="PoNazev" align="LEFT"><pos r="2" c1="5" c2="22" /></div>
            <div k="Stav" inf="ZobrazenoNast" align="LEFT"><pos r="-1" c1="0" c2="40" /></div>
          </t>
          <t k="Info" n="Info" cls="LCD1"><div k="Text" inf="TextLine1" align="LEFT"><pos c1="0" c2="123" /></div></t>
          <t k="Odchodova" n="Odchodova" cls="LCD1">
            <div k="Cas" inf="CasOdjezdu" align="LEFT"><pos c1="0" c2="5" /></div>
            <div k="Kolaj" inf="KolejOdjezd" align="RIGHT"><pos c1="5" c2="10" /></div>
          </t>
          <t k="Tyflo" n="Tyflo" cls="ERP">
            <div k="Druh" inf="TypVlaku" align="LEFT"><pos r="-1" c1="0" c2="20" /></div>
            <div k="Napis" inf="Free" align="LEFT"><pos c1="0" c2="230" /></div>
            <div k="Text" inf="TextLine1" align="LEFT"><pos c1="0" c2="230" /></div>
          </t>
          <t k="Elen" n="Elen" cls="ELEN16" />
        </CATALOG>
        """;

    private DirectoryInfo _root = null!;
    private string _program = null!;

    private string DataDir => Path.Combine(_root.FullName, "Server", "Data");

    private AppSettings Settings => new() { InissProgram = _program };

    [TestInitialize]
    public void Init()
    {
        _root = Directory.CreateTempSubdirectory("tsiniss2");
        _program = Path.Combine(_root.FullName, "Server", "IniSrv.exe");

        Write("Config.xml", """<INISSCFG GTNPort="1090" />""");
        Write("CommLines.xml", CommLines);
        Write("DirList.xml", """<DL><g n="2030"><i s="5400010" d="01 Lipovec" /><i s="5400020" d="02 Horany" /></g></DL>""");
        Write(@"2030\01 Lipovec\TCatalog.xml", Catalog);
        Write(@"2030\01 Lipovec\PositionsDef.xml", """
            <POZICE>
              <pos k="1" n="1" tabkolej="4" tabnast="II" />
              <pos k="2" n="2" tabkolej="4a" tabnast="II" />
            </POZICE>
            """);
        Write(@"2030\02 Horany\TCatalog.xml", Catalog);
        Write(@"2030\01 Lipovec\Tables.xml", """
            <TABLES>
              <t k="Odjezdy Internet" n="Lipovec" save="5400010O.xml"><part ck="Web" t="DEPARTURE" rec="10" /></t>
              <t k="Nastupiste 1" n="Nastupiste 1" lin="Tabule" addr="2">
                <part ck="Web" t="INFO" rec="1" />
                <part ck="Nastupistna" t="PLATFORM" rec="1"><pos k="1" /><pos k="2" /><pos k="X" /></part>
                <part ck="Info" t="INFO" rec="1" />
              </t>
              <t k="Odchody hala" n="Odchody hala" lin="Tabule" addr="3"><part ck="Odchodova" t="DEPARTURE" rec="5" /></t>
              <t k="AKIS" n="AKIS" lin="Tabule" addr="52"><part ck="Tyflo" t="PLATFORM" rec="1" /></t>
              <t k="Neznama linka" n="Neznama linka" lin="Zmazana" addr="9"><part ck="Nastupistna" t="PLATFORM" rec="1" /></t>
              <t k="Elen na ITP" n="Elen" lin="Tabule" addr="7"><part ck="Elen" t="PLATFORM" rec="1" /></t>
              <t k="Lcd1 na seriovej" n="Lcd" lin="Lcd" addr="3"><part ck="Nastupistna" t="PLATFORM" rec="1" /></t>
            </TABLES>
            """);
        Write(@"2030\02 Horany\Tables.xml", """
            <TABLES>
              <t k="Odjezdy Internet" n="Horany" save="5400020O.xml"><part ck="Web" t="DEPARTURE" rec="10" /></t>
              <t k="Nastupiste 1 znova" n="Nastupiste" lin="Tabule" addr="2"><part ck="Nastupistna" t="PLATFORM" rec="1" /></t>
            </TABLES>
            """);
        File.WriteAllBytes(_program, []);
    }

    [TestCleanup]
    public void Cleanup() => _root.Delete(true);

    private void Write(string relativeFile, string text)
    {
        var file = Path.Combine(DataDir, relativeFile);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text, Encodings.Win1250);
    }

    private InissStationDto Read() => InissStations.Read(Settings).Value;

    private RedirectInissLinesCommandHandler Handler(AppSettings? settings = null) =>
        new(new TestSettings(settings ?? Settings), NullLogger<RedirectInissLinesCommandHandler>.Instance);

    private string CommLinesText() => File.ReadAllText(Path.Combine(DataDir, "CommLines.xml"), Encodings.Win1250);

    [TestMethod]
    public void Read_RecognizesServerByItsFolder()
    {
        var station = Read();

        Assert.IsTrue(station.Iniss2);
        Assert.IsFalse(station.Registry);
        Assert.AreEqual("IniSrv", station.AppName);
        CollectionAssert.AreEqual(new[] { "2030" }, station.Grafikons.ToArray());
        Assert.AreEqual(Path.Combine(_root.FullName, "Server", "LOG"), station.LogFolder);
    }

    [TestMethod]
    public void Read_LinesGetNumberByOrderAndClassByProtocol()
    {
        var lines = Read().Lines;

        CollectionAssert.AreEqual(new int?[] { 1, 2, 3, 4, 5 }, lines.Select(l => l.Number).ToArray());
        Assert.AreEqual("Tabule", lines[0].Section);
        Assert.AreEqual(130, lines[0].TableClass);
        Assert.AreEqual("10.0.0.1:10001", lines[0].Port);
        Assert.AreEqual(47001, lines[0].SimulatorPort);
        Assert.AreEqual(StationLineTransport.Udp, lines[0].Transport);
        Assert.IsTrue(lines[0].CanRedirect);
        Assert.IsNull(lines[0].OriginalPort);

        Assert.IsFalse(lines[1].Supported, "AKSIGNAL nie je linka tabul");
        Assert.AreEqual(4, lines[2].TableClass);
        Assert.AreEqual(StationLineTransport.Tcp, lines[2].Transport);
        Assert.IsFalse(lines[3].Active);
        Assert.AreEqual(5, lines[4].TableClass, "linka LCD s tabulou LCD1");
        Assert.AreEqual(StationLineTransport.Tcp, lines[4].Transport, "seriova linka sa presmeruje na TCP");
        Assert.IsTrue(lines[4].CanRedirect);
    }

    [TestMethod]
    public void Read_TablesComeFromCatalogOfTheirStation()
    {
        var tables = Read().Lines[0].Tables;

        Assert.HasCount(4, tables, "tabula s rovnakou adresou v druhej stanici sa nezdvoji");
        Assert.AreEqual("Nastupiste 1", tables[0].Name);
        Assert.AreEqual(2, tables[0].Address);
        Assert.AreEqual("LCD1", tables[0].Manufacturer);
        Assert.IsTrue(tables[0].Shown);
        Assert.AreEqual("LCD1", tables[1].Manufacturer);
        Assert.AreEqual("ERP", tables[2].Manufacturer);
        Assert.AreEqual("2030", tables[2].Grafikon);
    }

    [TestMethod]
    public void Face_PlatformBoardFromItsParts()
    {
        var face = Read().Lines[0].Tables[0].Face!;

        Assert.AreEqual(StationBoardKind.Platform, face.Kind);
        Assert.AreEqual(4, face.Records * face.LinesPerRecord, "tri riadky predlohy a riadok informacneho textu");
        Assert.AreEqual(23 * 8, face.Width, "stlpce su v znakoch, informacny text sirku nemeni");
        Assert.AreEqual(16, face.RowHeight);
        Assert.AreEqual("Lipovec", face.Station, "bez poradia pred nazvom priecinka");
        CollectionAssert.AreEqual(new[] { "4", "4a" }, face.Tracks.ToArray(), "kolaj X stanica nepozna");
        CollectionAssert.AreEqual(new[] { "II" }, face.Platforms.ToArray());
    }

    [TestMethod]
    public void Face_ColumnsUseContentOfIniss3AndPoints()
    {
        var columns = Read().Lines[0].Tables[0].Face!.Columns;

        Assert.AreEqual(new StationFaceColumnDto(0, 136, 176, Contracts.Faces.FaceAlign.Center, 9), columns[1], "cas odchodu");
        Assert.AreEqual(new StationFaceColumnDto(1, 136, 160, Contracts.Faces.FaceAlign.Right, 12), columns[2], "meskanie odchodu");
        Assert.AreEqual(2, columns[^1].Line);
        Assert.IsFalse(columns.Any(c => c.Start == 0 && c.End == 320), "skryty udaj");
    }

    [TestMethod]
    public void Face_DepartureBoardCountsRecords()
    {
        var face = Read().Lines[0].Tables[1].Face!;

        Assert.AreEqual(StationBoardKind.Departures, face.Kind);
        Assert.AreEqual(5, face.Records * face.LinesPerRecord);
        Assert.AreEqual(80, face.Width);
    }

    [TestMethod]
    public void Face_TextOnlyBoardKeepsDefaultWidthAndItsRows()
    {
        var face = Read().Lines[0].Tables[2].Face!;

        Assert.AreEqual(320, face.Width, "sirka z predvolenej tabule");
        Assert.AreEqual(1, face.Records * face.LinesPerRecord);
        Assert.IsEmpty(face.Columns);
    }

    [TestMethod]
    public void Face_BuildsBoardFaces()
    {
        var tables = Read().Lines[0].Tables;

        var platform = StationFaces.Build(tables[0].Face!, false, StationFaceLanguage.Cs);
        var departures = StationFaces.Build(tables[1].Face!, false, StationFaceLanguage.Cs);

        Assert.AreEqual("4", platform?.Values[StationFaces.TrackKey], "z kolaji 4 a 4a ostava spolocny zaciatok");
        Assert.AreEqual("Lipovec", platform?.Values[StationFaces.StationKey]);
        Assert.IsTrue(departures?.Face.Elements.Any(e => e.Text.StartsWith("ODJEZD", StringComparison.Ordinal)), "popis stlpca");
    }

    [TestMethod]
    public void Import_UsesSizeFromCatalog()
    {
        var simulator = new Simulator();

        simulator.ImportStation(RequestMapper.ToImport(InissStations.ToRequest(Read())));

        var board = simulator.Read(s => s.Boards.Single(b => b.Line == 1 && b.Address == 2));
        Assert.AreEqual(23 * 8, board.Profile.Width);
        Assert.AreEqual(4, board.Profile.Rows);
    }

    [TestMethod]
    public void Read_ReportsProblems()
    {
        var station = Read();

        Assert.IsFalse(station.Lines[0].Tables[3].Shown);
        var family = station.Lines[0].Problems.Single();
        Assert.AreEqual(InissProblemCode.WrongFamily, family.Code);
        CollectionAssert.AreEqual(new[] { "Elen na ITP", "ELEN16", "130" }, family.Args.ToArray());

        Assert.AreEqual(InissProblemCode.NoTables, station.Lines[2].Problems.Single().Code);

        var unserved = station.Unserved.Single();
        Assert.AreEqual("Neznama linka", unserved.Name);
        Assert.AreEqual(InissProblemCode.UnknownLine, unserved.Problem!.Code);
        CollectionAssert.AreEqual(new[] { "Zmazana" }, unserved.Problem.Args.ToArray());
    }

    [TestMethod]
    public void Read_XmlTablesOfAllStations()
    {
        var xml = Read().XmlTables;

        CollectionAssert.AreEqual(new[] { "5400010O", "5400020O" }, xml.Select(t => t.Source).ToArray());
        Assert.AreEqual("Lipovec - Odjezdy Internet", xml[0].Name);
    }

    [TestMethod]
    public void Read_DamagedFileIsReported()
    {
        Write("CommLines.xml", "<CommLines>");

        var result = InissStations.Read(Settings);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("iniss.unreadable", result.Error.Code);
    }

    [TestMethod]
    public void Import_UsesTransportOfTheLine()
    {
        var simulator = new Simulator();
        var request = InissStations.ToRequest(Read());

        Assert.AreEqual(StationLineTransport.Udp, request.Lines[0].Transport);

        var result = simulator.ImportStation(RequestMapper.ToImport(request));

        Assert.Contains(2, result.UnsupportedLines);
        Assert.AreEqual(new LineEndpoint(LineTransport.Udp, "0.0.0.0", 47001), simulator.Read(s => s.FindLine(1)!.Endpoint));
        Assert.AreEqual(new LineEndpoint(LineTransport.Tcp, "0.0.0.0", 47003), simulator.Read(s => s.FindLine(3)!.Endpoint));
        Assert.AreEqual(3, simulator.Read(s => s.Boards.Count(b => b.Line == 1)), "tabula ELEN sa na linke ITP nezobrazi");
    }

    [TestMethod]
    public void Import_ChangedTransportReplacesEndpoint()
    {
        var simulator = new Simulator();
        simulator.ImportStation(new StationImport { Lines = [new StationLine(1, 130, 47001)] });

        simulator.ImportStation(new StationImport { Lines = [new StationLine(1, 130, 47001) { Transport = LineTransport.Udp }] });

        Assert.AreEqual(LineTransport.Udp, simulator.Read(s => s.FindLine(1)!.Endpoint.Transport));
    }

    [TestMethod]
    public async Task Redirect_RewritesOnlyLinesTheSimulatorShows()
    {
        var result = await Handler().Handle(new RedirectInissLinesCommand(new InissRedirectRequest(true)), CancellationToken.None);

        var lines = result.Value.Lines;
        Assert.AreEqual("127.0.0.1:47001", lines[0].Port);
        Assert.AreEqual("10.0.0.1:10001", lines[0].OriginalPort);
        Assert.AreEqual(47001, lines[0].SimulatorPort);
        Assert.AreEqual("127.0.0.1:47003", lines[2].Port);
        Assert.AreEqual("10.0.0.2:20", lines[1].Port, "linku zvuku simulator nezobrazi");
        Assert.AreEqual("10.0.0.4:40", lines[3].Port, "vynechana linka");
        Assert.AreEqual("127.0.0.1:47005", lines[4].Port, "seriova linka sa zmeni na TCP_CLIENT");
        Assert.AreEqual("COM1", lines[4].OriginalPort);
        Assert.AreEqual(StationLineTransport.Tcp, lines[4].Transport);
    }

    [TestMethod]
    public async Task Redirect_UndoRestoresTheFile()
    {
        await Handler().Handle(new RedirectInissLinesCommand(new InissRedirectRequest(true)), CancellationToken.None);
        Assert.AreNotEqual(CommLines, CommLinesText());

        var result = await Handler().Handle(new RedirectInissLinesCommand(new InissRedirectRequest(false)), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(CommLines, CommLinesText());
        Assert.IsNull(result.Value.Lines[0].OriginalPort);
    }

    [TestMethod]
    public async Task Redirect_PortStaysAfterRepeatedRead()
    {
        await Handler().Handle(new RedirectInissLinesCommand(new InissRedirectRequest(true)), CancellationToken.None);
        await Handler().Handle(new RedirectInissLinesCommand(new InissRedirectRequest(true)), CancellationToken.None);

        Assert.AreEqual(1, CommLinesText().Split("line=\"10.0.0.1:10001\"").Length - 1, "povodna adresa je v subore raz");
    }

    [TestMethod]
    public async Task Redirect_Iniss3IsRefused()
    {
        var dir = Directory.CreateDirectory(Path.Combine(_root.FullName, "Iniss339"));
        Directory.CreateDirectory(Path.Combine(dir.FullName, "DATA"));
        File.WriteAllText(Path.Combine(dir.FullName, "DATA", "DirList.txt"), "");
        var program = Path.Combine(dir.FullName, "INISSTEST" + Guid.NewGuid().ToString("N")[..8] + ".exe");
        File.WriteAllBytes(program, []);

        var result = await Handler(new AppSettings { InissProgram = program }).Handle(new RedirectInissLinesCommand(new InissRedirectRequest(true)), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("iniss.notIniss2", result.Error.Code);
    }

    private sealed class TestSettings(AppSettings settings) : ISettingsStore
    {
        public AppSettings Current { get; private set; } = settings;

        public void Save(AppSettings settings) => Current = settings;
    }
}
