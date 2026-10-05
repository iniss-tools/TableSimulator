using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Auth;
using TableSimulator.Contracts.Lines;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Server cez TestServer: rezim bez prihlasenia, prvy spravca, prihlasenie a zablokovanie, opravnenia roli, CSRF
/// a politika na kazdom endpointe. Nastavenie servera ide cez premenne prostredia - testy nebezia sucasne.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AuthApiTests
{
    private static SimulatorServer Start(bool requireLogin) => SimulatorServer.Start(requireLogin ? """{"requireLogin":true}""" : "{}");

    private static HttpClient Client(SimulatorServer server, bool csrfHeader = true) => server.Client(csrfHeader);

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static Task<HttpResponseMessage> Setup(HttpClient client, string password = "heslo-spravcu") =>
        client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("spravca", "Správca", password));

    [TestMethod]
    public async Task BezPrihlasenia_KazdyJeSpravca()
    {
        await using var server = Start(false);
        using var client = Client(server);

        var me = await client.GetFromJsonAsync<MeDto>("/api/auth/me");

        Assert.IsFalse(me!.LoginRequired);
        Assert.IsTrue(me.Authenticated);
        Assert.AreEqual(Roles.Admin, me.Role);
        CollectionAssert.Contains(me.Permissions.ToArray(), Permissions.UsersManage);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/state")).StatusCode);
    }

    [TestMethod]
    public async Task PrvySpravca_ZalozenieAPrihlasenie()
    {
        await using var server = Start(true);
        using var client = Client(server);

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/state")).StatusCode);
        Assert.IsTrue((await client.GetFromJsonAsync<MeDto>("/api/auth/me"))!.SetupRequired);

        var setup = await Setup(client);

        Assert.AreEqual(HttpStatusCode.OK, setup.StatusCode);
        var me = await setup.Content.ReadFromJsonAsync<MeDto>();
        Assert.AreEqual("spravca", me!.UserName);
        Assert.AreEqual(Roles.Admin, me.Role);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/state")).StatusCode, "cookie po zalozeni");

        var again = await Setup(Client(server));
        Assert.AreEqual(HttpStatusCode.Conflict, again.StatusCode);
        Assert.AreEqual("auth.setupDone", await CodeOf(again));
    }

    [TestMethod]
    public async Task SlabeHeslo_ChybaOverenia()
    {
        await using var server = Start(true);
        using var client = Client(server);

        var response = await Setup(client, "kratke");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("auth.password", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task BezHlavicky_ZmenaOdmietnuta()
    {
        await using var server = Start(false);
        using var client = Client(server, csrfHeader: false);

        var response = await client.PutAsJsonAsync("/api/lines/5", new SetLineRequest { TableClass = 4, Endpoint = "TCP://127.0.0.1:47005" });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual("auth.csrf", await CodeOf(response));
    }

    [TestMethod]
    public async Task Prihlasenie_ZleHesloAZablokovanie()
    {
        await using var server = Start(true);
        await Setup(Client(server));
        using var client = Client(server);

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("spravca", "zle-heslo", false));
        Assert.AreEqual(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.AreEqual("auth.invalidCredentials", await CodeOf(wrong));

        var good = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("spravca", "heslo-spravcu", false));
        Assert.AreEqual(HttpStatusCode.OK, good.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/state")).StatusCode);

        await client.PostAsync("/api/auth/logout", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/state")).StatusCode);

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("spravca", "zle-heslo", false));
        var locked = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("spravca", "heslo-spravcu", false));
        Assert.AreEqual("auth.lockedOut", await CodeOf(locked));
    }

    [TestMethod]
    public async Task Pozorovatel_VidiAleNemeni()
    {
        await using var server = Start(true);
        await Setup(Client(server));
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var viewer = new AppUser { UserName = "pozorovatel", DisplayName = "Pozorovateľ" };
            Assert.IsTrue((await users.CreateAsync(viewer, "heslo-pozorovatela")).Succeeded);
            await users.AddToRoleAsync(viewer, Roles.Viewer);
        }

        using var client = Client(server);
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("pozorovatel", "heslo-pozorovatela", true));
        var me = await login.Content.ReadFromJsonAsync<MeDto>();

        Assert.AreEqual(Roles.Viewer, me!.Role);
        CollectionAssert.DoesNotContain(me.Permissions.ToArray(), Permissions.SimulatorEdit);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/state")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/log")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync("/api/lines/5", new SetLineRequest { TableClass = 4, Endpoint = "TCP://127.0.0.1:47005" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/log")).StatusCode);
    }

    [TestMethod]
    public async Task KazdyEndpoint_MaPolitikuAleboJeVerejny()
    {
        await using var server = Start(false);

        var endpoints = server.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        Assert.IsGreaterThan(20, endpoints.Count);
        foreach (var endpoint in endpoints)
        {
            var secured = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
                          || endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => a.Policy is not null);
            Assert.IsTrue(secured, $"{endpoint.RoutePattern.RawText} nema politiku opravnenia");
        }
    }
}
