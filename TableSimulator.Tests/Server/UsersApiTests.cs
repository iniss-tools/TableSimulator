using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Audit;
using TableSimulator.Contracts.Auth;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Tests.Server;

/// <summary>Sprava pouzivatelov a audit cez API: zalozenie, zmena roly, heslo, vypnutie, zablokovanie, ochrany, audit.</summary>
[TestClass]
[DoNotParallelize]
public sealed class UsersApiTests
{
    private const string AdminPassword = "heslo-spravcu";

    private static SimulatorServer Start(bool requireLogin) => SimulatorServer.Start(requireLogin ? """{"requireLogin":true}""" : "{}");

    /// <summary>Server s prihlasovanim a prihlasenym prvym spravcom.</summary>
    private static async Task<(SimulatorServer Server, HttpClient Admin)> StartWithAdmin()
    {
        var server = Start(true);
        var admin = server.Client();
        var setup = await admin.PostAsJsonAsync("/api/auth/setup", new SetupRequest("spravca", "Správca", AdminPassword));
        Assert.AreEqual(HttpStatusCode.OK, setup.StatusCode);
        return (server, admin);
    }

    private static async Task<(HttpResponseMessage Response, HttpClient Client)> Login(SimulatorServer server, string userName, string password)
    {
        var client = server.Client();
        return (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(userName, password, false)), client);
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static async Task<UserDto> UserOf(HttpClient admin, string userName) =>
        (await admin.GetFromJsonAsync<UserDto[]>("/api/users"))!.Single(u => u.UserName == userName);

    [TestMethod]
    public async Task Sprava_ZalozenieRolaHesloVypnutieOdstranenie()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;

        Assert.AreEqual(HttpStatusCode.NoContent,
            (await admin.PostAsJsonAsync("/api/users", new CreateUserRequest("novak", "Ján Novák", Roles.Operator, "heslo-novaka"))).StatusCode);
        Assert.AreEqual("auth.userExists",
            await CodeOf(await admin.PostAsJsonAsync("/api/users", new CreateUserRequest("NOVAK", "", Roles.Viewer, "heslo-novaka"))));
        Assert.IsNull((await UserOf(admin, "novak")).LastLoginAt);

        var (login, novak) = await Login(server, "novak", "heslo-novaka");
        Assert.AreEqual(Roles.Operator, (await login.Content.ReadFromJsonAsync<MeDto>())!.Role);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await novak.GetAsync("/api/users")).StatusCode);
        Assert.IsNotNull((await UserOf(admin, "novak")).LastLoginAt);

        await admin.PutAsJsonAsync("/api/users/novak", new UpdateUserRequest("Ján Novák ml.", Roles.Viewer, false));
        var user = await UserOf(admin, "novak");
        Assert.AreEqual(("Ján Novák ml.", Roles.Viewer), (user.DisplayName, user.Role));

        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync("/api/users/novak/password", new SetPasswordRequest("nove-heslo-1"))).StatusCode);
        Assert.AreEqual("auth.password", await CodeOf(await admin.PostAsJsonAsync("/api/users/novak/password", new SetPasswordRequest("kratke"))));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(server, "novak", "heslo-novaka")).Response.StatusCode);
        Assert.AreEqual(Roles.Viewer, (await (await Login(server, "novak", "nove-heslo-1")).Response.Content.ReadFromJsonAsync<MeDto>())!.Role);

        await admin.PutAsJsonAsync("/api/users/novak", new UpdateUserRequest("", Roles.Viewer, true));
        Assert.AreEqual("auth.disabled", await CodeOf((await Login(server, "novak", "nove-heslo-1")).Response));

        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/users/novak")).StatusCode);
        Assert.AreEqual("users.notFound", await CodeOf(await admin.DeleteAsync("/api/users/novak")));
        Assert.AreEqual("auth.invalidCredentials", await CodeOf((await Login(server, "novak", "nove-heslo-1")).Response));
    }

    [TestMethod]
    public async Task Ochrana_SamSebaNeZmeniNeVypneNeOdstrani()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;

        Assert.AreEqual("users.self", await CodeOf(await admin.PutAsJsonAsync("/api/users/spravca", new UpdateUserRequest("Správca", Roles.Viewer, false))));
        Assert.AreEqual("users.self", await CodeOf(await admin.PutAsJsonAsync("/api/users/spravca", new UpdateUserRequest("Správca", Roles.Admin, true))));
        Assert.AreEqual("users.self", await CodeOf(await admin.DeleteAsync("/api/users/spravca")));
        // vlastne meno zmenit moze
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/users/spravca", new UpdateUserRequest("Hlavný správca", Roles.Admin, false))).StatusCode);
        // zmena hesla alebo vypnutia zrusi prihlasenia - overovanie razitka musi byt zaregistrovane
        using var scope = server.Services.CreateScope();
        Assert.IsNotNull(scope.ServiceProvider.GetService<ISecurityStampValidator>());
    }

    [TestMethod]
    public async Task Ochrana_PoslednySpravcaOstane()
    {
        await using var server = Start(false);
        using var local = server.Client();
        await local.PostAsJsonAsync("/api/users", new CreateUserRequest("a1", "", Roles.Admin, "heslo-prveho"));

        Assert.AreEqual("users.lastAdmin", await CodeOf(await local.PutAsJsonAsync("/api/users/a1", new UpdateUserRequest("", Roles.Viewer, false))));
        Assert.AreEqual("users.lastAdmin", await CodeOf(await local.PutAsJsonAsync("/api/users/a1", new UpdateUserRequest("", Roles.Admin, true))));
        Assert.AreEqual("users.lastAdmin", await CodeOf(await local.DeleteAsync("/api/users/a1")));

        await local.PostAsJsonAsync("/api/users", new CreateUserRequest("a2", "", Roles.Admin, "heslo-druheho"));
        Assert.AreEqual(HttpStatusCode.NoContent, (await local.PutAsJsonAsync("/api/users/a1", new UpdateUserRequest("", Roles.Operator, false))).StatusCode);
        Assert.AreEqual("users.lastAdmin", await CodeOf(await local.DeleteAsync("/api/users/a2")));
    }

    [TestMethod]
    public async Task Zablokovanie_SpravcaOdomkne()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        await admin.PostAsJsonAsync("/api/users", new CreateUserRequest("novak", "", Roles.Viewer, "heslo-novaka"));
        for (var i = 0; i < 5; i++)
            await Login(server, "novak", "zle-heslo");

        Assert.AreEqual("auth.lockedOut", await CodeOf((await Login(server, "novak", "heslo-novaka")).Response));
        Assert.IsNotNull((await UserOf(admin, "novak")).LockedUntil);

        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.PostAsync("/api/users/novak/unlock", null)).StatusCode);
        Assert.IsNull((await UserOf(admin, "novak")).LockedUntil);
        Assert.AreEqual(HttpStatusCode.OK, (await Login(server, "novak", "heslo-novaka")).Response.StatusCode);
    }

    [TestMethod]
    public async Task VlastneHeslo_OverenieAPrihlasenieOstane()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        await admin.PostAsJsonAsync("/api/users", new CreateUserRequest("novak", "", Roles.Viewer, "heslo-novaka"));
        var (_, novak) = await Login(server, "novak", "heslo-novaka");

        Assert.AreEqual("users.currentPassword",
            await CodeOf(await novak.PostAsJsonAsync("/api/auth/password", new ChangePasswordRequest("zle-heslo", "nove-heslo-1"))));
        Assert.AreEqual(HttpStatusCode.NoContent,
            (await novak.PostAsJsonAsync("/api/auth/password", new ChangePasswordRequest("heslo-novaka", "nove-heslo-1"))).StatusCode);

        Assert.IsTrue((await novak.GetFromJsonAsync<MeDto>("/api/auth/me"))!.Authenticated);
        Assert.AreEqual(HttpStatusCode.OK, (await Login(server, "novak", "nove-heslo-1")).Response.StatusCode);

        await using var local = Start(false);
        Assert.AreEqual("auth.notRequired",
            await CodeOf(await local.Client().PostAsJsonAsync("/api/auth/password", new ChangePasswordRequest("x", "nove-heslo-1"))));
    }

    [TestMethod]
    public async Task Audit_KtoCoUrobilAjNeuspesnePrihlasenie()
    {
        var (server, admin) = await StartWithAdmin();
        await using var _ = server;
        await Login(server, "nikto", "zle-heslo");
        await admin.PostAsJsonAsync("/api/users", new CreateUserRequest("novak", "", Roles.Operator, "heslo-novaka"));

        var page = (await admin.GetFromJsonAsync<AuditPageDto>("/api/audit"))!;

        var entries = page.Entries.Select(e => (e.User, e.Action, e.Target, e.Success, e.Error)).ToArray();
        CollectionAssert.AreEqual(new (string, string, string, bool, string?)[]
        {
            ("spravca", "CreateUser", "novak", true, null),
            ("-", "Login", "nikto", false, "auth.invalidCredentials"),
            ("spravca", "Setup", "spravca", true, null)
        }, entries);
        Assert.IsFalse(page.HasMore);
        StringAssert.Contains(page.Entries[0].Details, "\"password\":\"***\"");
        Assert.IsFalse(page.Entries.Any(e => e.Details.Contains("heslo-", StringComparison.Ordinal)));

        var first = (await admin.GetFromJsonAsync<AuditPageDto>("/api/audit?limit=1"))!;
        var next = (await admin.GetFromJsonAsync<AuditPageDto>($"/api/audit?limit=1&before={first.Entries[0].Id}&action=Login"))!;
        Assert.IsTrue(first.HasMore);
        Assert.AreEqual("nikto", next.Entries.Single().Target);

        var (_, novak) = await Login(server, "novak", "heslo-novaka");
        Assert.AreEqual(HttpStatusCode.Forbidden, (await novak.GetAsync("/api/audit")).StatusCode);
    }
}
