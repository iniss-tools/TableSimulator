using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Keys;
using TableSimulator.Contracts.Audit;
using TableSimulator.Contracts.Auth;
using TableSimulator.Contracts.Keys;
using TableSimulator.Contracts.Lines;

namespace TableSimulator.Tests.Server;

/// <summary>Pristupove kluce: program s rolou, zariadenie len na pozeranie, WebSocket, odstranenie, audit.</summary>
[TestClass]
[DoNotParallelize]
public sealed class AccessKeysApiTests
{
    private static async Task<(SimulatorServer Server, HttpClient Admin)> StartWithAdmin()
    {
        var server = SimulatorServer.Start("""{"requireLogin":true}""");
        var admin = server.Client();
        await admin.PostAsJsonAsync("/api/auth/setup", new SetupRequest("spravca", "Správca", "heslo-spravcu"));
        return (server, admin);
    }

    private static async Task<AccessKeyCreatedDto> Create(HttpClient admin, string name, AccessKeyKind kind, string role)
    {
        var response = await admin.PostAsJsonAsync("/api/keys", new CreateAccessKeyRequest(name, kind, role));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AccessKeyCreatedDto>())!;
    }

    /// <summary>Klient bez cookie a bez hlavicky CSRF - len s klucom.</summary>
    private static HttpClient WithKey(SimulatorServer server, string secret)
    {
        var client = server.Client(csrfHeader: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        return client;
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static SetLineRequest Pipe() => new() { TableClass = 4, Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N") };

    [TestMethod]
    public void Kluc_NahodnyAHashovany()
    {
        var a = AccessKeys.NewSecret();
        var b = AccessKeys.NewSecret();

        Assert.AreNotEqual(a, b);
        StringAssert.StartsWith(a, AccessKeys.SecretPrefix);
        Assert.AreEqual(46, a.Length);
        Assert.AreEqual(64, AccessKeys.Hash(a).Length);
        Assert.AreEqual(AccessKeys.Hash(a), AccessKeys.Hash(a));
    }

    [TestMethod]
    public async Task ApiKluc_RolaBezCookieABezCsrf()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        var created = await Create(admin, "GVDEditor", AccessKeyKind.Api, Roles.Operator);
        using var key = WithKey(server, created.Secret);

        var me = (await key.GetFromJsonAsync<MeDto>("/api/auth/me"))!;
        Assert.AreEqual(("key:GVDEditor", Roles.Operator, true), (me.UserName, me.Role, me.AccessKey));
        Assert.AreEqual(HttpStatusCode.OK, (await key.GetAsync("/api/state")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await key.PutAsJsonAsync("/api/lines/7", Pipe())).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await key.GetAsync("/api/users")).StatusCode);

        var keys = (await admin.GetFromJsonAsync<AccessKeyDto[]>("/api/keys"))!;
        Assert.AreEqual(created.Secret[..AccessKeys.PrefixLength], keys.Single().Prefix);
        Assert.IsNotNull(keys.Single().LastUsedAt);
        Assert.AreEqual("spravca", keys.Single().CreatedBy);

        var audit = (await admin.GetFromJsonAsync<AuditPageDto>("/api/audit"))!;
        Assert.AreEqual(("key:GVDEditor", "SetLine", "7"), (audit.Entries[0].User, audit.Entries[0].Action, audit.Entries[0].Target));
        Assert.IsFalse(audit.Entries.Any(e => e.Details.Contains(created.Secret, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task ZariadenieLenPozera_OdstranenyKlucNeplati()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        // kluc zariadenia ma vzdy rolu pozorovatel, aj keby sa ziadal spravca
        var device = await Create(admin, "Monitor v hale", AccessKeyKind.Device, Roles.Admin);
        using var key = WithKey(server, device.Secret);

        Assert.AreEqual(Roles.Viewer, device.Key.Role);
        Assert.AreEqual(HttpStatusCode.OK, (await key.GetAsync("/api/virtual")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await key.PutAsJsonAsync("/api/lines/7", Pipe())).StatusCode);

        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/keys/{device.Key.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await key.GetAsync("/api/virtual")).StatusCode);
        Assert.IsFalse((await key.GetFromJsonAsync<MeDto>("/api/auth/me"))!.Authenticated);
        Assert.AreEqual("keys.notFound", await CodeOf(await admin.DeleteAsync($"/api/keys/{device.Key.Id}")));
    }

    [TestMethod]
    public async Task NeplatnyKluc_ZamietnutyAjSCookie()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "ts_neplatny");

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/state")).StatusCode);
    }

    [TestMethod]
    public async Task WebSocket_KlucVParametri()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        var device = await Create(admin, "Kiosk", AccessKeyKind.Device, Roles.Viewer);
        var client = server.Server.CreateWebSocketClient();

        using var socket = await client.ConnectAsync(new Uri(server.Server.BaseAddress, $"ws?access_token={device.Secret}"), CancellationToken.None);
        Assert.AreEqual(System.Net.WebSockets.WebSocketState.Open, socket.State);

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ConnectAsync(new Uri(server.Server.BaseAddress, "ws?access_token=ts_neplatny"), CancellationToken.None));
        StringAssert.Contains(refused.Message, "401");
    }
}
