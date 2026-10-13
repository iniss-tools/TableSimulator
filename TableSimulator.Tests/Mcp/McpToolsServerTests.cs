using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using TableSimulator.Contracts.Audit;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Keys;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Scenarios;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Protocols.Apel;
using TableSimulator.Core.Simulation;
using TableSimulator.Tests.Server;
using static TableSimulator.Tests.Archive.ArchiveReaderTests;
using static TableSimulator.Tests.Server.RecordingComparisonApiTests;
using static TableSimulator.Tests.Virtual.XmlTableTests;

namespace TableSimulator.Tests.Mcp;

/// <summary>
/// Kazdy nastroj MCP nad stanicou s udajmi: vysledok zodpoveda scheme vystupu a obsahuje, co ma; chyby s kodom pre model.
/// Novy nastroj treba pridat do <see cref="KazdyNastroj_VysledokPodlaSchemyAUdajov" />.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class McpToolsServerTests
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Pripojeny klient a schemy vystupu nastrojov; zaznamenava volane nastroje.</summary>
    private sealed class Session(McpClient client, Dictionary<string, JsonElement> schemas, HashSet<string> writeTools, Simulator simulator, TestContext test)
        : IAsyncDisposable
    {
        public HashSet<string> Called { get; } = [];

        public IReadOnlyCollection<string> Tools => schemas.Keys;

        /// <summary>Nastroje, ktore nie su len na citanie (podla priznaku pre klienta).</summary>
        public IReadOnlyCollection<string> WriteTools => writeTools;

        public Simulator Simulator => simulator;

        public McpClient Client => client;

        public async Task<JsonElement> Call(string tool, Dictionary<string, object?>? arguments = null)
        {
            var result = await client.CallToolAsync(tool, arguments, cancellationToken: test.CancellationToken);
            Assert.IsFalse(result.IsError == true, $"{tool}: {Text(result)}");
            var content = result.StructuredContent!.Value;
            var errors = OutputSchema.Validate(content, schemas[tool]);
            Assert.IsEmpty(errors, $"{tool}: {string.Join("; ", errors)}");
            Called.Add(tool);
            return content;
        }

        /// <summary>Chyba nastroja - text, ktory dostane model.</summary>
        public async Task<string> Error(string tool, Dictionary<string, object?> arguments)
        {
            var result = await client.CallToolAsync(tool, arguments, cancellationToken: test.CancellationToken);
            Assert.IsTrue(result.IsError, $"{tool} mal skoncit chybou: {Text(result)}");
            Called.Add(tool);
            return Text(result);
        }

        private static string Text(CallToolResult result) => string.Join(" ", result.Content.OfType<TextContentBlock>().Select(t => t.Text));

        public ValueTask DisposeAsync() => client.DisposeAsync();
    }

    /// <summary>
    /// Server so stanicou: linka 7 (APEL) s tabulou 7-129 "Praha", zaznamy "pred" a "po", archiv tabul a hlaseni, scenar
    /// poruch; klient s klucom roly <paramref name="role" />.
    /// </summary>
    private async Task<(SimulatorServer Server, Session Mcp)> StartWithData(string role = "viewer")
    {
        var server = SimulatorServer.Start();
        var logs = Path.Combine(server.DataFolder, "Logy");
        Directory.CreateDirectory(logs);
        await File.WriteAllTextAsync(Path.Combine(server.DataFolder, "settings.json"), JsonSerializer.Serialize(new { virtualFolders = new[] { logs } }),
            TestContext.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(logs, "TAB2026-10-04.XML"), ArchiveBytes(
            TrainLine,
            """<T d="2026-10-04" c="14:10:00" s="Dolné Mesto" t="R" v="601" jm=""><t><n>NAST1</n><c n="Čas" s="14:12"/></t></T>""",
            """<T d="2026-10-04" c="14:20:00" s="Dolné Mesto" t="Os" v="7815" jm=""><t><n>NAST1</n><c n="Čas" s="14:25"/></t></T>"""),
            TestContext.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(logs, "REP2026-10-03.XML"), ArchiveBytes(
            """<H d="2026-10-03" c="22:00:00" u="hlásenie" n="Príchod" s="Dolné Mesto" t="Os" v="7813" kp="1" zp="0" e="OK"/>"""),
            TestContext.CancellationToken);

        await File.WriteAllBytesAsync(Path.Combine(logs, "DolneMestoO.xml"), Xml(Row("11:35", "7813", "Bratislava#Wien") + Row("11:52", "601")),
            TestContext.CancellationToken);

        var admin = server.Client();
        var virtualBoard = new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO", RowStart = 1, Separator = ", " };
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/virtual/5", virtualBoard, TestContext.CancellationToken)).StatusCode);
        var line = new SetLineRequest { TableClass = 10, Manufacturer = "APEL", Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N") };
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/lines/7", line, TestContext.CancellationToken)).StatusCode);
        var simulator = server.Services.GetRequiredService<Simulator>();
        // samoucenie zalozi tabulu 7-129
        simulator.Receive(7, ApelFrame.Build(0x81, new ApelRecord(0, "Praha"u8.ToArray())));
        // hodiny tabule (DateTime bez pasma) - vo vystupe nesmie mat format date-time
        simulator.Receive(7, Convert.FromHexString("100281810705301401101020" + "26B01003"));
        foreach (var (name, content) in new[]
                 {
                     ("pred", RecordingComparisonApiTests.Recording((0, Row(3, 1, "Os 1234 Žilina")), (10, Row(3, 1, "Os 1236 Košice")), (12, Row(5, 1, "Pokladňa")))),
                     ("po", RecordingComparisonApiTests.Recording((3, Row(3, 1, "Os 1234 Žilina")), (14, Row(3, 1, "Os 1238 Prešov")), (15, Row(7, 1, "Nová"))))
                 })
        {
            var upload = await admin.PostAsync($"/api/recordings/upload?name={name}.tsrec", new ByteArrayContent(content), TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, upload.StatusCode);
        }

        var scenario = new FaultScenarioRequest("Vypadok", false, null, [new ScenarioStepDto(0, 1000, 7, 129, ScenarioAction.Fault, new BoardFaultDto(FaultKind.NoReply))]);
        Assert.AreEqual(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/scenarios", scenario, TestContext.CancellationToken)).StatusCode);
        var key = await admin.PostAsJsonAsync("/api/keys", new CreateAccessKeyRequest("Agent " + role, AccessKeyKind.Api, role), TestContext.CancellationToken);
        var secret = (await key.Content.ReadFromJsonAsync<AccessKeyCreatedDto>(TestContext.CancellationToken))!.Secret;

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.Server.BaseAddress, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + secret }
        }, server.CreateClient(), ownsHttpClient: true);
        var client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.CancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
        return (server, new Session(client, tools.ToDictionary(t => t.Name, t => t.ProtocolTool.OutputSchema!.Value),
            tools.Where(t => t.ProtocolTool.Annotations?.ReadOnlyHint != true).Select(t => t.Name).ToHashSet(), simulator, TestContext));
    }

    [TestMethod]
    public async Task KazdyNastroj_VysledokPodlaSchemyAUdajov()
    {
        var (server, mcp) = await StartWithData();
        await using var serverScope = server;
        await using var mcpScope = mcp;

        var status = await mcp.Call("get_status");
        Assert.AreEqual(1, status.GetProperty("lines")[0].GetProperty("boards").GetInt32());

        var boards = (await mcp.Call("list_boards")).GetProperty("boards");
        Assert.AreEqual("7-129", boards[0].GetProperty("id").GetString());
        Assert.AreEqual("Praha", boards[0].GetProperty("rows")[0].GetString());
        var compact = (await mcp.Call("list_boards", new() { ["line"] = 7, ["includeText"] = false })).GetProperty("boards");
        Assert.AreEqual(JsonValueKind.Null, compact[0].GetProperty("rows").ValueKind);

        var board = await mcp.Call("get_board", new() { ["id"] = "7-129", ["raw"] = true });
        Assert.AreEqual("Praha", board.GetProperty("rows")[0].GetString());
        Assert.AreEqual("chars", board.GetProperty("family").GetString());
        Assert.AreEqual("2026-10-01T14:30:05", board.GetProperty("clock").GetString());
        Assert.AreEqual(JsonValueKind.Object, board.GetProperty("display").ValueKind);
        var version = board.GetProperty("version").GetInt64();

        var unchanged = await mcp.Call("wait_for_board_change", new() { ["id"] = "7-129", ["afterVersion"] = version, ["timeoutSeconds"] = 1 });
        Assert.IsFalse(unchanged.GetProperty("changed").GetBoolean());
        var waiting = mcp.Call("wait_for_board_change", new() { ["id"] = "7-129", ["afterVersion"] = version, ["timeoutSeconds"] = 30 });
        await Task.Delay(200, TestContext.CancellationToken);
        mcp.Simulator.Receive(7, ApelFrame.Build(0x81, new ApelRecord(0, "Brno"u8.ToArray())));
        var changed = await waiting;
        Assert.IsTrue(changed.GetProperty("changed").GetBoolean());
        Assert.AreEqual("Brno", changed.GetProperty("board").GetProperty("rows")[0].GetString());

        Assert.IsGreaterThan(0, (await mcp.Call("get_frames", new() { ["line"] = 7 })).GetProperty("frames").GetArrayLength());

        Assert.AreEqual(2, (await mcp.Call("list_recordings")).GetProperty("items").GetArrayLength());
        var comparison = await mcp.Call("compare_recordings", new() { ["a"] = "pred", ["b"] = "po" });
        Assert.AreEqual(1, comparison.GetProperty("different").GetInt32());
        Assert.AreEqual(1, comparison.GetProperty("onlyA").GetInt32());
        Assert.AreEqual(1, comparison.GetProperty("onlyB").GetInt32());
        Assert.AreEqual(3, comparison.GetProperty("boards").GetArrayLength());
        var timeline = await mcp.Call("get_recording_board", new() { ["name"] = "pred", ["board"] = "3-3" });
        Assert.AreEqual(2, timeline.GetProperty("states").GetArrayLength());
        var at = await mcp.Call("get_recording_board", new() { ["name"] = "pred", ["board"] = "3-3", ["time"] = 11.0 });
        Assert.AreEqual("Os 1236 Košice", at.GetProperty("states")[0].GetProperty("rows").EnumerateArray().Last().GetString());

        var tables = await mcp.Call("get_table_archive");
        Assert.AreEqual("2026-10-04", tables.GetProperty("date").GetString());
        // tri zaznamy dna, s vlakom 78xx dva
        Assert.AreEqual(3, tables.GetProperty("total").GetInt32());
        Assert.AreEqual(2, (await mcp.Call("get_table_archive", new() { ["train"] = "78" })).GetProperty("total").GetInt32());
        var reports = await mcp.Call("get_report_archive");
        Assert.AreEqual("2026-10-03", reports.GetProperty("date").GetString());
        Assert.AreEqual(1, reports.GetProperty("total").GetInt32());
        await mcp.Call("get_recent_reports");

        await mcp.Call("get_traffic_statistics", new() { ["series"] = true });

        // vzhlad: vstavane vzhlady a predlohy, pisma bez priecinka pisiem
        var designs = (await mcp.Call("list_designs")).GetProperty("designs").EnumerateArray().Select(d => d.GetProperty("id").GetString()).ToList();
        CollectionAssert.Contains(designs, "elen-led");
        var design = await mcp.Call("get_design", new() { ["id"] = "elen-led" });
        Assert.IsTrue(design.GetProperty("builtIn").GetBoolean());
        Assert.AreEqual("led-matrix", design.GetProperty("design").GetProperty("renderer").GetString());
        var faceId = (await mcp.Call("list_faces")).GetProperty("faces")[0].GetProperty("id").GetString();
        Assert.AreEqual(faceId, (await mcp.Call("get_face", new() { ["id"] = faceId })).GetProperty("id").GetString());
        // virtualne tabule: server prechadza priecinky kazde 2 s
        var overview = await mcp.Call("list_virtual_boards");
        Assert.AreEqual(5, overview.GetProperty("boards")[0].GetProperty("id").GetInt32());
        JsonElement content = default;
        for (var i = 0; i < 50; i++)
        {
            content = await mcp.Call("get_virtual_board", new() { ["id"] = 5 });
            if (content.GetProperty("source").ValueKind == JsonValueKind.Object)
                break;
            await Task.Delay(100, TestContext.CancellationToken);
        }

        Assert.AreEqual(2, content.GetProperty("rows").GetArrayLength());
        StringAssert.Contains(content.GetProperty("rows")[0].ToString(), "7813");
        StringAssert.EndsWith(content.GetProperty("viewUrl").GetString(), "/#/virtual/5");
        var layoutId = content.GetProperty("layout").GetString();
        Assert.AreEqual(layoutId, (await mcp.Call("get_virtual_layout", new() { ["id"] = layoutId })).GetProperty("id").GetString());

        // MCP Apps: show_board so zobrazenim, board_view len pre aplikaciu, zdroj ui://
        var tools = await mcp.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
        var showMeta = tools.Single(t => t.Name == "show_board").ProtocolTool.Meta!;
        Assert.AreEqual("ui://tablesimulator/board.html", showMeta["ui"]!["resourceUri"]!.GetValue<string>());
        var viewMeta = tools.Single(t => t.Name == "board_view").ProtocolTool.Meta!;
        Assert.AreEqual("app", viewMeta["ui"]!["visibility"]![0]!.GetValue<string>());
        var resources = await mcp.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken);
        var app = resources.Single(r => r.Uri == "ui://tablesimulator/board.html");
        Assert.AreEqual("text/html;profile=mcp-app", app.MimeType);
        var html = await mcp.Client.ReadResourceAsync("ui://tablesimulator/board.html", cancellationToken: TestContext.CancellationToken);
        StringAssert.Contains(((TextResourceContents)html.Contents[0]).Text, "ui/initialize");
        var shown = await mcp.Call("show_board", new() { ["id"] = "7-129" });
        var noChange = await mcp.Call("board_view", new() { ["id"] = "7-129", ["afterVersion"] = shown.GetProperty("version").GetInt64(), ["timeoutSeconds"] = 1 });
        Assert.IsFalse(noChange.GetProperty("changed").GetBoolean());
        // prvy pohlad hned: tabula, vzhlad (APEL = apel-led) a celo bez cakania
        var first = await mcp.Call("board_view", new() { ["id"] = "7-129", ["waitForChange"] = false });
        Assert.IsTrue(first.GetProperty("changed").GetBoolean());
        Assert.AreEqual("7-129", first.GetProperty("board").GetProperty("id").GetString());
        Assert.AreEqual("led-matrix", first.GetProperty("design").GetProperty("renderer").GetString());
        Assert.AreEqual(JsonValueKind.Null, first.GetProperty("font").ValueKind);

        var fonts = await mcp.Call("list_fonts");
        Assert.AreEqual(0, fonts.GetProperty("files").GetArrayLength());
        CollectionAssert.Contains(fonts.GetProperty("charFonts").EnumerateArray().Select(f => f.GetString()).ToList(), "7x14");
        Assert.AreEqual("Vypadok", (await mcp.Call("list_scenarios")).GetProperty("scenarios")[0].GetProperty("name").GetString());

        CollectionAssert.AreEquivalent(mcp.Tools.ToArray(), mcp.Called.ToArray(), "kazdy nastroj na citanie musi byt v tomto teste");
        // pozorovatel nastroje so zapisom nevidi
        Assert.IsEmpty(mcp.WriteTools);
    }

    [TestMethod]
    public async Task ZapisoveNastroje_ZmenyAAudit()
    {
        var (server, mcp) = await StartWithData("operator");
        await using var serverScope = server;
        await using var mcpScope = mcp;
        var pipe = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N");

        // linka a tabula: nova s povinnymi hodnotami, zmena len zadanych
        var line = await mcp.Call("set_line", new() { ["number"] = 8, ["tableClass"] = 10, ["endpoint"] = pipe, ["name"] = "Skuska" });
        Assert.AreEqual(10, line.GetProperty("tableClass").GetInt32());
        var renamed = await mcp.Call("set_line", new() { ["number"] = 8, ["name"] = "Skuska 2" });
        Assert.AreEqual("Skuska 2", renamed.GetProperty("name").GetString());
        StringAssert.Contains(renamed.GetProperty("endpoint").GetString(), "TableSimulatorTest");
        StringAssert.Contains(await mcp.Error("set_line", new() { ["number"] = 9, ["name"] = "Bez triedy" }), "line.new");
        var board = await mcp.Call("set_board", new() { ["line"] = 8, ["address"] = 129, ["manufacturer"] = "APEL", ["name"] = "Vestibul" });
        Assert.AreEqual("8-129", board.GetProperty("id").GetString());
        var renamedBoard = await mcp.Call("set_board", new() { ["line"] = 8, ["address"] = 129, ["name"] = "Vestibul 2" });
        Assert.AreEqual("APEL", renamedBoard.GetProperty("manufacturer").GetString());
        Assert.AreEqual("Vestibul 2", renamedBoard.GetProperty("name").GetString());

        // agent hra INISS; porucha bez odpovede, vymazanie
        var sent = await mcp.Call("send_frame", new() { ["line"] = 8, ["hex"] = Apel("Brno") });
        Assert.AreEqual(1, sent.GetProperty("replies").GetArrayLength());
        Assert.AreEqual("Brno", sent.GetProperty("boards")[0].GetProperty("rows")[0].GetString());
        var fault = await mcp.Call("set_board_fault", new() { ["id"] = "8-129", ["kind"] = "noReply" });
        Assert.AreEqual("noReply", fault.GetProperty("fault").GetProperty("kind").GetString());
        Assert.AreEqual(0, (await mcp.Call("send_frame", new() { ["line"] = 8, ["hex"] = Apel("Praha") })).GetProperty("replies").GetArrayLength());
        await mcp.Call("set_board_fault", new() { ["id"] = "8-129", ["kind"] = "none" });
        var cleared = await mcp.Call("clear_board", new() { ["id"] = "8-129" });
        Assert.IsTrue(cleared.GetProperty("rows").EnumerateArray().All(r => r.GetString()!.Length == 0));

        // scenar: vypadok napajania tabule
        object[] steps = [new { atMs = 0, durationMs = 60000, line = 8, address = 129, action = "powerLoss", fault = (object?)null }];
        var scenario = await mcp.Call("save_scenario", new() { ["name"] = "Vypadok tabule", ["steps"] = steps });
        var id = scenario.GetProperty("id").GetInt32();
        var saved = await mcp.Call("save_scenario", new() { ["id"] = id, ["name"] = "Vypadok 2", ["steps"] = steps });
        Assert.AreEqual("Vypadok 2", saved.GetProperty("name").GetString());
        Assert.AreEqual(JsonValueKind.Object, (await mcp.Call("start_scenario", new() { ["id"] = id })).GetProperty("run").ValueKind);
        var poweredOff = false;
        for (var i = 0; i < 30 && !poweredOff; i++)
        {
            await Task.Delay(100, TestContext.CancellationToken);
            poweredOff = (await mcp.Call("get_board", new() { ["id"] = "8-129" })).GetProperty("poweredOff").GetBoolean();
        }

        Assert.IsTrue(poweredOff, "scenar mal tabulu vypnut");
        Assert.AreEqual(JsonValueKind.Null, (await mcp.Call("stop_scenario", new() { ["id"] = id })).GetProperty("run").ValueKind);

        // zaznam vlastnej skusky a jeho prehratie
        Assert.IsTrue((await mcp.Call("start_recording", new() { ["note"] = "Skuska MCP" })).GetProperty("recording").GetBoolean());
        await mcp.Call("send_frame", new() { ["line"] = 8, ["hex"] = Apel("Zilina") });
        Assert.IsFalse((await mcp.Call("stop_recording")).GetProperty("recording").GetBoolean());
        var recording = (await mcp.Call("list_recordings")).GetProperty("items").EnumerateArray().Single(r => r.GetProperty("note").GetString() == "Skuska MCP");
        await mcp.Call("play_recording", new() { ["name"] = recording.GetProperty("name").GetString(), ["speed"] = 10 });
        await mcp.Call("stop_replay");

        // vzhlad tabule: kopia vzhladu a predlohy, ciastocna zmena profilu, pouzita predloha sa neda zmazat
        var builtInFace = (await mcp.Call("list_faces")).GetProperty("faces").EnumerateArray().First(f => f.GetProperty("builtIn").GetBoolean())
            .GetProperty("id").GetString();
        var myDesign = await mcp.Call("save_design", new() { ["id"] = "moj-vzhlad", ["copyFrom"] = "elen-led", ["name"] = "Môj vzhľad" });
        Assert.IsFalse(myDesign.GetProperty("builtIn").GetBoolean());
        Assert.AreEqual("Môj vzhľad", myDesign.GetProperty("design").GetProperty("name").GetString());
        var myFace = await mcp.Call("save_face", new() { ["id"] = "moje-celo", ["copyFrom"] = builtInFace, ["name"] = "Moje čelo" });
        Assert.IsFalse(myFace.GetProperty("builtIn").GetBoolean());
        var look = await mcp.Call("set_board_look", new()
        {
            ["id"] = "8-129", ["design"] = "moj-vzhlad",
            ["profile"] = new { face = "moje-celo", faceValues = new { track = "1" }, charFonts = new Dictionary<string, object> { ["17"] = new { font = "7x14", bold = true } } }
        });
        Assert.AreEqual("moj-vzhlad", look.GetProperty("design").GetString());
        Assert.AreEqual("moje-celo", look.GetProperty("profile").GetProperty("face").GetString());
        Assert.AreEqual("7x14", look.GetProperty("profile").GetProperty("charFonts").GetProperty("17").GetProperty("font").GetString());
        StringAssert.EndsWith(look.GetProperty("viewUrl").GetString(), "/#/view/8-129");
        StringAssert.Contains(await mcp.Error("set_board_look", new() { ["id"] = "8-129", ["profile"] = new { charFonts = new Dictionary<string, object> { ["99"] = new { font = "x" } } } }),
            "board.charFonts");
        StringAssert.Contains(await mcp.Error("delete_face", new() { ["id"] = "moje-celo" }), "face.inUse");
        Assert.AreEqual("moj-vzhlad", look.GetProperty("designInUse").GetString());
        StringAssert.Contains(await mcp.Error("set_board_look", new() { ["id"] = "8-129", ["design"] = "neexistuje" }), "designs.notFound");
        var reset = await mcp.Call("set_board_look", new() { ["id"] = "8-129", ["defaultDesign"] = true, ["profile"] = new Dictionary<string, object?> { ["face"] = null } });
        Assert.AreEqual("apel-led", reset.GetProperty("designInUse").GetString());
        Assert.AreEqual(JsonValueKind.Null, reset.GetProperty("profile").GetProperty("face").ValueKind);
        Assert.AreEqual(JsonValueKind.Null, reset.GetProperty("design").ValueKind);
        await mcp.Call("delete_face", new() { ["id"] = "moje-celo" });
        await mcp.Call("delete_design", new() { ["id"] = "moj-vzhlad" });

        // virtualna tabula: nova, ciastocna zmena, null = predvolena hodnota; rozlozenie z kopie
        var created = await mcp.Call("save_virtual_board", new() { ["id"] = 6, ["changes"] = new { name = "Podchod", source = "DolneMestoO", rowCount = 1 } });
        Assert.AreEqual(1, created.GetProperty("board").GetProperty("rowCount").GetInt32());
        var allRows = await mcp.Call("save_virtual_board", new() { ["id"] = 6, ["changes"] = new Dictionary<string, object?> { ["rowCount"] = null } });
        Assert.AreEqual(JsonValueKind.Null, allRows.GetProperty("board").GetProperty("rowCount").ValueKind);
        Assert.AreEqual("Podchod", allRows.GetProperty("board").GetProperty("name").GetString());
        var layout = await mcp.Call("save_virtual_layout", new() { ["id"] = "moje-odchody", ["copyFrom"] = "odchody", ["changes"] = new { name = "Moje", title = "Odchody vlakov" } });
        Assert.AreEqual("Odchody vlakov", layout.GetProperty("title").GetString());
        Assert.IsFalse(layout.GetProperty("builtIn").GetBoolean());
        StringAssert.Contains(await mcp.Error("save_virtual_layout", new() { ["id"] = "odchody", ["changes"] = new { title = "X" } }), "virtual.layoutBuiltIn");
        Assert.AreEqual("moje-odchody", (await mcp.Call("save_virtual_board", new() { ["id"] = 6, ["changes"] = new { layout = "moje-odchody" } })).GetProperty("layout").GetString());
        StringAssert.Contains(await mcp.Error("delete_virtual_layout", new() { ["id"] = "moje-odchody" }), "virtual.layoutInUse");
        await mcp.Call("delete_virtual_board", new() { ["id"] = 6 });
        await mcp.Call("delete_virtual_layout", new() { ["id"] = "moje-odchody" });

        // stanica z INISSu bez programu INISSu v nastaveni
        StringAssert.Contains(await mcp.Error("get_iniss_station", new()), "iniss.notConfigured");
        StringAssert.Contains(await mcp.Error("import_iniss_station", new()), "iniss.notConfigured");

        await mcp.Call("delete_board", new() { ["id"] = "8-129" });
        await mcp.Call("delete_line", new() { ["number"] = 8 });
        StringAssert.Contains(await mcp.Error("get_board", new() { ["id"] = "8-129" }), "board.notFound");

        // kazda zmena v audite pod menom kluca
        var audit = await server.Client().GetFromJsonAsync<AuditPageDto>("/api/audit?limit=200", TestContext.CancellationToken);
        var actions = audit!.Entries.Where(e => e.User == "key:Agent operator").Select(e => e.Action).ToHashSet();
        CollectionAssert.IsSubsetOf(new[] { "SetLine", "SetBoard", "SendFrame", "SetBoardFault", "ClearBoard", "CreateFaultScenario", "SetFaultScenario",
            "StartFaultScenario", "StopFaultScenario", "PlayRecording", "SetDesign", "DeleteDesign", "SetFace", "DeleteFace", "SetVirtualBoard", "SetVirtualLayout", "DeleteVirtualBoard", "DeleteVirtualLayout",
            "DeleteBoard", "DeleteLine" },
            actions.ToArray());
        CollectionAssert.IsSubsetOf(mcp.WriteTools.ToArray(), mcp.Called.ToArray(), "kazdy nastroj so zapisom musi byt v tomto teste");
        CollectionAssert.Contains(mcp.Called.ToArray(), "get_iniss_station");
    }

    /// <summary>Sprava APEL s textom prveho riadku tabule 129 ako hex.</summary>
    private static string Apel(string text) => Convert.ToHexString(ApelFrame.Build(0x81, new ApelRecord(0, System.Text.Encoding.ASCII.GetBytes(text))));

    [TestMethod]
    public async Task Chyby_KodPreModel()
    {
        var (server, mcp) = await StartWithData();
        await using var serverScope = server;
        await using var mcpScope = mcp;

        StringAssert.Contains(await mcp.Error("get_board", new() { ["id"] = "7-1" }), "board.notFound");
        StringAssert.Contains(await mcp.Error("wait_for_board_change", new() { ["id"] = "7-129", ["afterVersion"] = 0, ["timeoutSeconds"] = 0 }), "board.waitTimeout");
        StringAssert.Contains(await mcp.Error("compare_recordings", new() { ["a"] = "pred", ["b"] = "po", ["board"] = "9-9" }), "recording.boardNotFound");
        StringAssert.Contains(await mcp.Error("get_recording_board", new() { ["name"] = "nie", ["board"] = "3-3" }), "recording.notFound");
        StringAssert.Contains(await mcp.Error("get_table_archive", new() { ["limit"] = 0 }), "archive.limit");
        StringAssert.Contains(await mcp.Error("get_table_archive", new() { ["date"] = "2026-01-01" }), "archive.notFound");
        StringAssert.Contains(await mcp.Error("get_traffic_statistics", new() { ["from"] = "2026-10-02T00:00:00Z", ["to"] = "2026-10-01T00:00:00Z" }), "statistics.range");
    }
}
