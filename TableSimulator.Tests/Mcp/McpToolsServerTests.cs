using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Keys;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Scenarios;
using TableSimulator.Core.Protocols.Apel;
using TableSimulator.Core.Simulation;
using TableSimulator.Tests.Server;
using static TableSimulator.Tests.Archive.ArchiveReaderTests;
using static TableSimulator.Tests.Server.RecordingComparisonApiTests;

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
    private sealed class Session(McpClient client, Dictionary<string, JsonElement> schemas, Simulator simulator, TestContext test) : IAsyncDisposable
    {
        public HashSet<string> Called { get; } = [];

        public IReadOnlyCollection<string> Tools => schemas.Keys;

        public Simulator Simulator => simulator;

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
            return Text(result);
        }

        private static string Text(CallToolResult result) => string.Join(" ", result.Content.OfType<TextContentBlock>().Select(t => t.Text));

        public ValueTask DisposeAsync() => client.DisposeAsync();
    }

    /// <summary>
    /// Server so stanicou: linka 7 (APEL) s tabulou 7-129 "Praha", zaznamy "pred" a "po", archiv tabul a hlaseni, scenar
    /// poruch; klient s klucom pozorovatela.
    /// </summary>
    private async Task<(SimulatorServer Server, Session Mcp)> StartWithData()
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

        var admin = server.Client();
        var line = new SetLineRequest { TableClass = 10, Manufacturer = "APEL", Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N") };
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/lines/7", line, TestContext.CancellationToken)).StatusCode);
        var simulator = server.Services.GetRequiredService<Simulator>();
        // samoucenie zalozi tabulu 7-129
        simulator.Receive(7, ApelFrame.Build(0x81, new ApelRecord(0, "Praha"u8.ToArray())));
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
        var key = await admin.PostAsJsonAsync("/api/keys", new CreateAccessKeyRequest("Agent", AccessKeyKind.Api, "viewer"), TestContext.CancellationToken);
        var secret = (await key.Content.ReadFromJsonAsync<AccessKeyCreatedDto>(TestContext.CancellationToken))!.Secret;

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.Server.BaseAddress, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + secret }
        }, server.CreateClient(), ownsHttpClient: true);
        var client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.CancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
        return (server, new Session(client, tools.ToDictionary(t => t.Name, t => t.ProtocolTool.OutputSchema!.Value), simulator, TestContext));
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
        Assert.AreEqual("Vypadok", (await mcp.Call("list_scenarios")).GetProperty("scenarios")[0].GetProperty("name").GetString());

        CollectionAssert.AreEquivalent(mcp.Tools.ToArray(), mcp.Called.ToArray(), "kazdy nastroj musi byt v tomto teste");
    }

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
