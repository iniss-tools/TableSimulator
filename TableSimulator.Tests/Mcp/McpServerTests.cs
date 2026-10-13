using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using TableSimulator.Contracts.Keys;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Roles;
using TableSimulator.Core.Simulation;
using TableSimulator.Tests.Server;

namespace TableSimulator.Tests.Mcp;

/// <summary>Server MCP na /mcp: pristup len s klucom, Origin, nastroje podla opravneni roly.</summary>
[TestClass]
[DoNotParallelize]
public sealed class McpServerTests
{
    public TestContext TestContext { get; set; } = null!;

    private static async Task<string> CreateKey(HttpClient admin, string role)
    {
        var response = await admin.PostAsJsonAsync("/api/keys", new CreateAccessKeyRequest("Agent " + role, AccessKeyKind.Api, role));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AccessKeyCreatedDto>())!.Secret;
    }

    private static async Task<McpClient> Connect(SimulatorServer server, string secret)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.Server.BaseAddress, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + secret }
        }, server.CreateClient(), ownsHttpClient: true);
        return await McpClient.CreateAsync(transport);
    }

    /// <summary>Uvodna sprava MCP bez klienta SDK - pre stav HTTP pri odmietnuti.</summary>
    private static HttpRequestMessage Initialize(string? secret, string? origin = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}""",
                Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (secret is not null)
            request.Headers.Authorization = new("Bearer", secret);
        if (origin is not null)
            request.Headers.Add("Origin", origin);
        return request;
    }

    [TestMethod]
    public async Task Pristup_BezKlucaOdmietnutyAjBezPrihlasovania()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.CreateClient();

        using var response = await client.SendAsync(Initialize(null), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Pristup_ZlyKlucOdmietnuty()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.CreateClient();

        using var response = await client.SendAsync(Initialize("ts_zly"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Pristup_CudziOriginOdmietnuty()
    {
        await using var server = SimulatorServer.Start();
        var secret = await CreateKey(server.Client(), "viewer");
        using var client = server.CreateClient();

        using var foreign = await client.SendAsync(Initialize(secret, "http://evil.example"), TestContext.CancellationToken);
        using var same = await client.SendAsync(Initialize(secret, "http://localhost"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, same.StatusCode);
    }

    [TestMethod]
    public async Task Pozorovatel_VidiNastrojeAVolaGetStatus()
    {
        await using var server = SimulatorServer.Start();
        var admin = server.Client();
        var secret = await CreateKey(admin, "viewer");
        var line = new SetLineRequest { TableClass = 4, Name = "Nastupistia", Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N") };
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/lines/7", line, TestContext.CancellationToken)).StatusCode);
        await using var mcp = await Connect(server, secret);

        var tools = await mcp.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
        var result = await mcp.CallToolAsync("get_status", cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual("TableSimulator", mcp.ServerInfo.Name);
        Assert.IsFalse(string.IsNullOrEmpty(mcp.ServerInstructions));
        CollectionAssert.IsSubsetOf(new[] { "get_frames", "get_status" }, tools.Select(t => t.Name).ToArray());
        Assert.IsNotNull(result.StructuredContent);
        var lines = result.StructuredContent.Value.GetProperty("lines");
        Assert.AreEqual(7, lines[0].GetProperty("number").GetInt32());
        Assert.AreEqual(JsonValueKind.String, lines[0].GetProperty("status").ValueKind);
        // schema uvadza vsetky vlastnosti ako povinne - null sa nesmie vynechat
        Assert.AreEqual(JsonValueKind.Null, lines[0].GetProperty("lastReceivedAt").ValueKind);
    }

    [TestMethod]
    public async Task GetFrames_FilterAPokracovanie()
    {
        await using var server = SimulatorServer.Start();
        var admin = server.Client();
        var secret = await CreateKey(admin, "viewer");
        var line = new SetLineRequest { TableClass = 4, Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N") };
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/lines/7", line, TestContext.CancellationToken)).StatusCode);
        var simulator = server.Services.GetRequiredService<Simulator>();
        simulator.Receive(7, "nezmysel"u8);
        await using var mcp = await Connect(server, secret);

        var first = await mcp.CallToolAsync("get_frames", new Dictionary<string, object?> { ["problemsOnly"] = true }, cancellationToken: TestContext.CancellationToken);
        var next = first.StructuredContent!.Value.GetProperty("next").GetInt64();
        var none = await mcp.CallToolAsync("get_frames", new Dictionary<string, object?> { ["after"] = next }, cancellationToken: TestContext.CancellationToken);
        var badLimit = await mcp.CallToolAsync("get_frames", new Dictionary<string, object?> { ["limit"] = 0 }, cancellationToken: TestContext.CancellationToken);

        var frames = first.StructuredContent.Value.GetProperty("frames");
        Assert.IsGreaterThan(0, frames.GetArrayLength());
        Assert.AreNotEqual("ok", frames[0].GetProperty("status").GetString());
        Assert.AreEqual(0, none.StructuredContent!.Value.GetProperty("frames").GetArrayLength());
        // chyba nastroja s kodom - model ju vidi a moze volanie opravit
        Assert.IsTrue(badLimit.IsError);
        StringAssert.Contains(JsonSerializer.Serialize(badLimit.Content, McpJsonUtilities.DefaultOptions), "frames.limit");
    }

    [TestMethod]
    public async Task RolaBezLogu_NevidiGetFramesANevola()
    {
        await using var server = SimulatorServer.Start();
        var admin = server.Client();
        var role = await admin.PostAsJsonAsync("/api/roles", new SetRoleRequest("Bez logu", ["simulator.view"]), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, role.StatusCode);
        var secret = await CreateKey(admin, (await role.Content.ReadFromJsonAsync<RoleCreatedDto>(TestContext.CancellationToken))!.Name);
        await using var mcp = await Connect(server, secret);

        var tools = (await mcp.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Select(t => t.Name).ToArray();
        await Assert.ThrowsAsync<McpException>(() => mcp.CallToolAsync("get_frames", cancellationToken: TestContext.CancellationToken).AsTask());

        CollectionAssert.Contains(tools, "get_status");
        CollectionAssert.DoesNotContain(tools, "get_frames");
    }
}
