using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Auth;
using TableSimulator.Contracts.Keys;
using TableSimulator.Contracts.Roles;
using TableSimulator.Contracts.Users;
using TableSimulator.Persistence;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Vlastne roly s opravneniami: zalozenie, pridelenie, okamzita zmena opravneni, ochrany vstavanych roli a pravidlo,
/// ze pouzivatel neprideli ani nespravuje viac, nez sam ma.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RolesApiTests
{
    private const string AdminPassword = "heslo-spravcu";

    private static async Task<(SimulatorServer Server, HttpClient Admin)> StartWithAdmin(bool deleteFolder = true)
    {
        var server = SimulatorServer.Start("""{"requireLogin":true}""", deleteFolder);
        var admin = server.Client();
        Assert.AreEqual(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/auth/setup", new SetupRequest("spravca", "Správca", AdminPassword))).StatusCode);
        return (server, admin);
    }

    private static async Task<HttpClient> Login(SimulatorServer server, string userName, string password)
    {
        var client = server.Client();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(userName, password, false));
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode, await login.Content.ReadAsStringAsync());
        return client;
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static async Task<string> CreateRole(HttpClient client, string displayName, params string[] permissions)
    {
        var response = await client.PostAsJsonAsync("/api/roles", new SetRoleRequest(displayName, permissions));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<RoleCreatedDto>())!.Name;
    }

    private static async Task CreateUser(HttpClient admin, string userName, string role, string password)
    {
        var response = await admin.PostAsJsonAsync("/api/users", new CreateUserRequest(userName, "", role, password));
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Roly_VstavaneAVlastneOpravnenia()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;

        var roles = (await admin.GetFromJsonAsync<RoleDto[]>("/api/roles"))!;
        CollectionAssert.AreEqual(new[] { Roles.Admin, Roles.Operator, Roles.Viewer }, roles.Select(r => r.Name).ToArray());
        CollectionAssert.AreEquivalent(Permissions.All.ToArray(), roles[0].Permissions.ToArray());
        CollectionAssert.AreEquivalent(Roles.DefaultsOf(Roles.Viewer).ToArray(), roles[2].Permissions.ToArray());
        Assert.AreEqual((true, 1), (roles[0].BuiltIn, roles[0].Users));

        var panels = await CreateRole(admin, "Informačné panely", Permissions.SimulatorView, Permissions.SignageManage);
        Assert.AreEqual("informacne-panely", panels);
        Assert.AreEqual("roles.duplicate", await CodeOf(await admin.PostAsJsonAsync("/api/roles", new SetRoleRequest("informačné PANELY", [Permissions.SimulatorView]))));
        Assert.AreEqual("roles.view", await CodeOf(await admin.PostAsJsonAsync("/api/roles", new SetRoleRequest("Bez prehľadu", [Permissions.SignageManage]))));
        Assert.AreEqual("roles.permissions", await CodeOf(await admin.PostAsJsonAsync("/api/roles", new SetRoleRequest("Zlé", [Permissions.SimulatorView, "simulator.edit"]))));

        await CreateUser(admin, "jana", panels, "heslo-jany-1");
        var jana = await Login(server, "jana", "heslo-jany-1");
        var me = (await jana.GetFromJsonAsync<MeDto>("/api/auth/me"))!;
        Assert.AreEqual((panels, "Informačné panely"), (me.Role, me.RoleName));
        CollectionAssert.AreEquivalent(new[] { Permissions.SimulatorView, Permissions.SignageManage }, me.Permissions.ToArray());
        Assert.AreEqual(HttpStatusCode.OK, (await jana.GetAsync("/api/state")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await jana.GetAsync("/api/statistics?from=2026-10-10T00:00:00Z&to=2026-10-10T01:00:00Z")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await jana.DeleteAsync("/api/lines/5")).StatusCode);

        // zmena opravneni roly plati hned, bez noveho prihlasenia
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/roles/{panels}",
            new SetRoleRequest("Panely a štatistiky", [Permissions.SimulatorView, Permissions.SignageManage, Permissions.StatisticsView]))).StatusCode);
        Assert.AreNotEqual(HttpStatusCode.Forbidden, (await jana.GetAsync("/api/statistics?from=2026-10-10T00:00:00Z&to=2026-10-10T01:00:00Z")).StatusCode);
        Assert.AreEqual("Panely a štatistiky", (await jana.GetFromJsonAsync<MeDto>("/api/auth/me"))!.RoleName);

        // vstavane roly: spravca sa neda upravit, vstavana rola zmazat; pouzivana rola sa neda zmazat
        Assert.AreEqual("roles.admin", await CodeOf(await admin.PutAsJsonAsync("/api/roles/admin", new SetRoleRequest("x", [Permissions.SimulatorView]))));
        Assert.AreEqual("roles.builtIn", await CodeOf(await admin.DeleteAsync("/api/roles/viewer")));
        Assert.AreEqual("roles.inUse", await CodeOf(await admin.DeleteAsync($"/api/roles/{panels}")));
        Assert.AreEqual("roles.notFound", await CodeOf(await admin.DeleteAsync("/api/roles/neznama")));
        Assert.AreEqual(1, (await admin.GetFromJsonAsync<RoleDto[]>("/api/roles"))!.Single(r => r.Name == panels).Users);

        // upravena vstavana rola si nazov necha (dava ho rozhranie)
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/roles/viewer", new SetRoleRequest("Iný", [Permissions.SimulatorView]))).StatusCode);
        var viewer = (await admin.GetFromJsonAsync<RoleDto[]>("/api/roles"))!.Single(r => r.Name == Roles.Viewer);
        Assert.AreEqual(("", 1), (viewer.DisplayName, viewer.Permissions.Count));

        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/users/jana")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/roles/{panels}")).StatusCode);
        Assert.IsFalse((await admin.GetFromJsonAsync<RoleDto[]>("/api/roles"))!.Any(r => r.Name == panels));
    }

    [TestMethod]
    public async Task Roly_NikdoNepridelViacNezSamMa()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        var people = await CreateRole(admin, "Správa ľudí", Permissions.SimulatorView, Permissions.UsersManage, Permissions.KeysManage);
        await CreateUser(admin, "hr", people, "heslo-hr-123");
        var hr = await Login(server, "hr", "heslo-hr-123");

        // vlastnu alebo nizsiu rolu prideli, vyssiu nie
        Assert.AreEqual(HttpStatusCode.NoContent, (await hr.PostAsJsonAsync("/api/users", new CreateUserRequest("kolega", "", people, "heslo-kolegu"))).StatusCode);
        Assert.AreEqual("users.roleTooHigh", await CodeOf(await hr.PostAsJsonAsync("/api/users", new CreateUserRequest("op", "", Roles.Operator, "heslo-operatora"))));
        // spravcovi heslo nenastavi ani ho nezmaze
        Assert.AreEqual("users.roleTooHigh", await CodeOf(await hr.PostAsJsonAsync("/api/users/spravca/password", new SetPasswordRequest("ukradnute-heslo"))));
        Assert.AreEqual("users.roleTooHigh", await CodeOf(await hr.DeleteAsync("/api/users/spravca")));
        // rolu s opravnenim, ktore nema, nezalozi ani neupravi; vlastnu rolu neupravi
        Assert.AreEqual("roles.tooHigh", await CodeOf(await hr.PostAsJsonAsync("/api/roles", new SetRoleRequest("Technik", [Permissions.SimulatorView, Permissions.LinesManage]))));
        Assert.AreEqual("roles.tooHigh", await CodeOf(await hr.PutAsJsonAsync("/api/roles/viewer", new SetRoleRequest("", [Permissions.SimulatorView]))));
        Assert.AreEqual("roles.self", await CodeOf(await hr.PutAsJsonAsync($"/api/roles/{people}", new SetRoleRequest("Správa ľudí", [Permissions.SimulatorView]))));
        var small = await CreateRole(hr, "Len pohľad", Permissions.SimulatorView);
        // kluc programu: s vyssou rolou nie
        Assert.AreEqual("keys.roleTooHigh", await CodeOf(await hr.PostAsJsonAsync("/api/keys", new CreateAccessKeyRequest("GVDEditor", AccessKeyKind.Api, Roles.Operator))));
        Assert.AreEqual(HttpStatusCode.OK, (await hr.PostAsJsonAsync("/api/keys", new CreateAccessKeyRequest("Skript", AccessKeyKind.Api, small))).StatusCode);
        Assert.AreEqual(1, (await admin.GetFromJsonAsync<RoleDto[]>("/api/roles"))!.Single(r => r.Name == small).Keys);
    }

    [TestMethod]
    public async Task Roly_StaraDatabazaDostanePredvoleneOpravnenia()
    {
        string folder;
        var (server, _) = await StartWithAdmin(deleteFolder: false);
        await using (server)
            folder = server.DataFolder;

        // rola zo starsej verzie bez opravneni v databaze
        await using (var db = ServerDatabase.Open(Path.Combine(folder, "tablesimulator.db")))
            await db.Roles.Where(r => r.Name == Roles.Operator).ExecuteUpdateAsync(r => r.SetProperty(x => x.Permissions, (List<string>?)null));

        await using var reopened = SimulatorServer.Open(folder, true);
        var admin = await Login(reopened, "spravca", AdminPassword);
        var op = (await admin.GetFromJsonAsync<RoleDto[]>("/api/roles"))!.Single(r => r.Name == Roles.Operator);
        CollectionAssert.AreEquivalent(Roles.DefaultsOf(Roles.Operator).ToArray(), op.Permissions.ToArray());
    }
}
