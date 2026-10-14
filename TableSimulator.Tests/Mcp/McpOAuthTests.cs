using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Auth;
using TableSimulator.Contracts.OAuth;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Persistence.Identity;
using TableSimulator.Tests.Server;

namespace TableSimulator.Tests.Mcp;

/// <summary>Prihlasenie klienta MCP cez OAuth: registracia, povolenie s rolou na stranke webu, tokeny a prepojene aplikacie.</summary>
[TestClass]
[DoNotParallelize]
public sealed class McpOAuthTests
{
    /// <summary>Overovaci kod PKCE a jeho vyzva z RFC 7636.</summary>
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
    private const string Redirect = "http://127.0.0.1:47123/callback";
    private const string WebPage = "/#/authorize/";

    public TestContext TestContext { get; set; } = null!;

    /// <summary>Prehliadac pouzivatela - presmerovanie spat ku klientovi necha na test; zmeny v /api s hlavickou CSRF.</summary>
    private static HttpClient Browser(SimulatorServer server)
    {
        var browser = server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        browser.DefaultRequestHeaders.Add(CsrfHeader.HeaderName, "TableSimulator");
        return browser;
    }

    /// <summary>Adresa povolenia klienta presmeruje na stranku webu - identifikator ziadosti z nej.</summary>
    private async Task<string> RequestId(HttpClient browser, string authorization)
    {
        using var response = await browser.GetAsync(authorization, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        StringAssert.StartsWith(location, WebPage);
        return location[WebPage.Length..];
    }

    /// <summary>Rozhodnutie na stranke webu - adresa navratu ku klientovi.</summary>
    private async Task<Uri> Decide(HttpClient browser, string requestId, bool allow, string? role)
    {
        using var response = await browser.PostAsJsonAsync($"/api/oauth/requests/{requestId}", new OAuthDecisionRequest(allow, role), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return new Uri((await response.Content.ReadFromJsonAsync<OAuthRedirectDto>(TestContext.CancellationToken))!.RedirectUrl);
    }

    private static string AuthorizationUrl(string clientId) =>
        $"/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(Redirect)}&code_challenge={Challenge}&code_challenge_method=S256&state=s";

    private async Task<string> Register(HttpClient browser)
    {
        using var registered = await browser.PostAsJsonAsync("/oauth/register", new { client_name = "Agent", redirect_uris = new[] { Redirect } },
            TestContext.CancellationToken);
        return (await registered.Content.ReadFromJsonAsync<JsonElement>(TestContext.CancellationToken)).GetProperty("client_id").GetString()!;
    }

    /// <summary>Registracia, povolenie s rolou a vymena kodu - odpoved /oauth/token a client_id.</summary>
    private async Task<(JsonElement Tokens, string ClientId)> Authorize(HttpClient browser, string role)
    {
        var clientId = await Register(browser);
        var back = await Decide(browser, await RequestId(browser, AuthorizationUrl(clientId)), true, role);
        var code = System.Web.HttpUtility.ParseQueryString(back.Query)["code"]!;
        var tokens = await Token(browser,
            new() { ["grant_type"] = "authorization_code", ["code"] = code, ["client_id"] = clientId, ["redirect_uri"] = Redirect, ["code_verifier"] = Verifier });
        Assert.AreEqual(HttpStatusCode.OK, tokens.Status);
        return (tokens.Body, clientId);
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> Token(HttpClient http, Dictionary<string, string> form)
    {
        using var response = await http.PostAsync("/oauth/token", new FormUrlEncodedContent(form), TestContext.CancellationToken);
        return (response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.CancellationToken));
    }

    /// <summary>Stav odpovede /mcp na uvodnu spravu s tokenom.</summary>
    private async Task<HttpStatusCode> McpStatus(HttpClient http, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}""",
                Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (token is not null)
            request.Headers.Authorization = new("Bearer", token);
        using var response = await http.SendAsync(request, TestContext.CancellationToken);
        return response.StatusCode;
    }

    [TestMethod]
    public async Task OAuth_KlientSdkSaPrihlasiSRolouZPovolenia()
    {
        // bez prihlasovania je pouzivatel prehliadaca spravca
        await using var server = SimulatorServer.Start();
        using var browser = Browser(server);
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.Server.BaseAddress, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            OAuth = new ClientOAuthOptions
            {
                RedirectUri = new Uri(Redirect),
                DynamicClientRegistration = new DynamicClientRegistrationOptions { ClientName = "Skusobny agent" },
                AuthorizationCallbackHandler = async (callback, _) =>
                {
                    var id = await RequestId(browser, callback.AuthorizationUri.PathAndQuery);
                    var request = await browser.GetFromJsonAsync<OAuthRequestDto>($"/api/oauth/requests/{id}", TestContext.CancellationToken);
                    Assert.AreEqual("Skusobny agent", request!.ClientName);
                    Assert.AreEqual("http://127.0.0.1:47123", request.RedirectHost);
                    var back = await Decide(browser, id, true, Roles.Viewer);
                    StringAssert.StartsWith(back.ToString(), Redirect + "?code=");
                    var query = System.Web.HttpUtility.ParseQueryString(back.Query);
                    return new AuthorizationResult { Code = query["code"], State = query["state"], Iss = query["iss"] };
                }
            }
        }, server.CreateClient(), ownsHttpClient: true);

        await using var client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.CancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);

        // rola pozorovatel z povolenia - len nastroje na citanie
        Assert.IsTrue(tools.Any(t => t.Name == "get_board"));
        Assert.IsFalse(tools.Any(t => t.ProtocolTool.Annotations?.ReadOnlyHint == false));
    }

    [TestMethod]
    public async Task OAuth_ZamietnutieVratiKlientoviChybu()
    {
        await using var server = SimulatorServer.Start();
        using var browser = Browser(server);
        var id = await RequestId(browser, AuthorizationUrl(await Register(browser)));

        var back = System.Web.HttpUtility.ParseQueryString((await Decide(browser, id, false, null)).Query);
        using var again = await browser.PostAsJsonAsync($"/api/oauth/requests/{id}", new OAuthDecisionRequest(true, null), TestContext.CancellationToken);

        Assert.AreEqual("access_denied", back["error"]);
        Assert.AreEqual("s", back["state"]);
        Assert.AreEqual("http://localhost", back["iss"]);
        // ziadost plati len na jedno rozhodnutie
        Assert.AreEqual(HttpStatusCode.NotFound, again.StatusCode);
    }

    [TestMethod]
    public async Task OAuth_ObnovenieVymeniTokenyAStaryTokenPovolenieZrusi()
    {
        await using var server = SimulatorServer.Start();
        using var browser = Browser(server);
        var (first, clientId) = await Authorize(browser, Roles.Viewer);
        var refresh = first.GetProperty("refresh_token").GetString()!;

        var second = await Token(browser, new() { ["grant_type"] = "refresh_token", ["refresh_token"] = refresh, ["client_id"] = clientId });
        var oldAccess = await McpStatus(browser, first.GetProperty("access_token").GetString());
        var newAccess = await McpStatus(browser, second.Body.GetProperty("access_token").GetString());
        // uz vymeneny obnovovaci token = unik: odmietnutie a koniec povolenia aj pre novy token
        var reused = await Token(browser, new() { ["grant_type"] = "refresh_token", ["refresh_token"] = refresh, ["client_id"] = clientId });
        var afterLeak = await McpStatus(browser, second.Body.GetProperty("access_token").GetString());

        Assert.AreEqual(HttpStatusCode.OK, second.Status);
        Assert.AreEqual(3600, second.Body.GetProperty("expires_in").GetInt32());
        Assert.AreEqual(HttpStatusCode.Unauthorized, oldAccess);
        Assert.AreEqual(HttpStatusCode.OK, newAccess);
        Assert.AreEqual(HttpStatusCode.BadRequest, reused.Status);
        Assert.AreEqual("invalid_grant", reused.Body.GetProperty("error").GetString());
        Assert.AreEqual(HttpStatusCode.Unauthorized, afterLeak);
    }

    [TestMethod]
    public async Task OAuth_TokenPlatiAjPreGetADelete()
    {
        await using var server = SimulatorServer.Start();
        using var browser = Browser(server);
        var access = (await Authorize(browser, Roles.Viewer)).Tokens.GetProperty("access_token").GetString();

        // VS Code otvara aj prud sprav servera (GET) - 401 by ho poslalo prihlasovat znova
        using var get = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        get.Headers.Accept.ParseAdd("text/event-stream");
        get.Headers.Authorization = new("Bearer", access);
        using var response = await browser.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, TestContext.CancellationToken);
        using var delete = new HttpRequestMessage(HttpMethod.Delete, "/mcp");
        delete.Headers.Authorization = new("Bearer", access);
        using var deleted = await browser.SendAsync(delete, TestContext.CancellationToken);
        using var anonymous = await browser.GetAsync("/mcp", TestContext.CancellationToken);

        // server bez relacie prud sprav nema (405); bez tokenu vyzva s odkazom na prihlasenie
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, deleted.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        StringAssert.Contains(anonymous.Headers.WwwAuthenticate.ToString(), "resource_metadata=");
    }

    [TestMethod]
    public async Task OAuth_ZlyOverovaciKodAleboNavratOdmietnuty()
    {
        await using var server = SimulatorServer.Start();
        using var browser = Browser(server);
        using var registered = await browser.PostAsJsonAsync("/oauth/register", new { client_name = "Agent", redirect_uris = new[] { "http://example.com/callback" } },
            TestContext.CancellationToken);
        using var foreign = await browser.GetAsync(
            "/oauth/authorize?response_type=code&client_id=nieje&redirect_uri=https%3A%2F%2Fevil.example&code_challenge=x&code_challenge_method=S256",
            TestContext.CancellationToken);
        var badVerifier = await Token(browser,
            new() { ["grant_type"] = "authorization_code", ["code"] = "x", ["client_id"] = "y", ["redirect_uri"] = Redirect, ["code_verifier"] = "z" });

        // http mimo tohto pocitaca sa ako navrat zaregistrovat neda
        Assert.AreEqual(HttpStatusCode.BadRequest, registered.StatusCode);
        // neznamy klient - len stranka s chybou, ziadne presmerovanie na cudziu adresu
        Assert.AreEqual(WebPage + "invalid", foreign.Headers.Location!.OriginalString);
        Assert.AreEqual("invalid_grant", badVerifier.Body.GetProperty("error").GetString());
    }

    [TestMethod]
    public async Task OAuth_RolaNajviacPouzivatelovaAVypnutyUcetPristupZrusi()
    {
        await using var server = SimulatorServer.Start("""{"requireLogin":true}""");
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var operatorUser = new AppUser { UserName = "dispecer", DisplayName = "Dispečer" };
            Assert.IsTrue((await users.CreateAsync(operatorUser, "heslo-dispecera")).Succeeded);
            await users.AddToRoleAsync(operatorUser, Roles.Operator);
        }
        using var browser = Browser(server);
        var id = await RequestId(browser, AuthorizationUrl(await Register(browser)));
        using var anonymous = await browser.GetAsync($"/api/oauth/requests/{id}", TestContext.CancellationToken);
        using var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("dispecer", "heslo-dispecera", false), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);

        // stranka ponukne len roly, ktore pouzivatel pokryva; vyssiu server odmietne a ziadost ostane na inu volbu
        var request = await browser.GetFromJsonAsync<OAuthRequestDto>($"/api/oauth/requests/{id}", TestContext.CancellationToken);
        using var admin = await browser.PostAsJsonAsync($"/api/oauth/requests/{id}", new OAuthDecisionRequest(true, Roles.Admin), TestContext.CancellationToken);
        await Decide(browser, id, true, Roles.Viewer);
        var (tokens, grantClient) = await Authorize(browser, Roles.Viewer);
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByNameAsync("dispecer"))!;
            user.IsDisabled = true;
            await users.UpdateAsync(user);
        }
        var refreshed = await Token(browser,
            new() { ["grant_type"] = "refresh_token", ["refresh_token"] = tokens.GetProperty("refresh_token").GetString()!, ["client_id"] = grantClient });

        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.AreEqual(Roles.Operator, request!.Role);
        CollectionAssert.Contains(request.Roles.Select(r => r.Name).ToArray(), Roles.Viewer);
        CollectionAssert.DoesNotContain(request.Roles.Select(r => r.Name).ToArray(), Roles.Admin);
        Assert.AreEqual(HttpStatusCode.Forbidden, admin.StatusCode);
        Assert.AreEqual("invalid_grant", refreshed.Body.GetProperty("error").GetString());
    }

    [TestMethod]
    public async Task OAuth_PrepojeneAplikacieZoznamAZrusenie()
    {
        await using var server = SimulatorServer.Start();
        using var browser = Browser(server);
        var access = (await Authorize(browser, Roles.Operator)).Tokens.GetProperty("access_token").GetString();

        var grants = await browser.GetFromJsonAsync<OAuthGrantDto[]>("/api/oauth/grants", TestContext.CancellationToken);
        using var deleted = await browser.DeleteAsync($"/api/oauth/grants/{grants![0].Id}", TestContext.CancellationToken);
        var after = await browser.GetFromJsonAsync<OAuthGrantDto[]>("/api/oauth/grants", TestContext.CancellationToken);

        Assert.HasCount(1, grants);
        Assert.AreEqual("Agent", grants[0].ClientName);
        Assert.AreEqual("local", grants[0].UserName);
        Assert.AreEqual(Roles.Operator, grants[0].Role);
        Assert.AreEqual(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.IsEmpty(after!);
        // zrusene povolenie - token prestane platit hned
        Assert.AreEqual(HttpStatusCode.Unauthorized, await McpStatus(browser, access));
    }

    [TestMethod]
    public async Task OAuth_PovoleniePrezijeRestartServera()
    {
        string access;
        string folder;
        await using (var server = SimulatorServer.Start(deleteFolder: false))
        {
            folder = server.DataFolder;
            using var browser = Browser(server);
            access = (await Authorize(browser, Roles.Operator)).Tokens.GetProperty("access_token").GetString()!;
        }

        await using var restarted = SimulatorServer.Open(folder, deleteFolder: true);
        using var http = restarted.CreateClient();

        Assert.AreEqual(HttpStatusCode.OK, await McpStatus(http, access));
    }

    [TestMethod]
    public async Task OAuth_VerejneAdresyMajuCors()
    {
        // agent VS Code vola opis servera z okna (prehliadac) - bez CORS by odpoved zahodil
        await using var server = SimulatorServer.Start();
        using var http = server.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/.well-known/oauth-authorization-server");
        preflight.Headers.Add("Origin", "vscode-file://vscode-app");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        preflight.Headers.Add("Access-Control-Request-Headers", "mcp-protocol-version");
        using var options = await http.SendAsync(preflight, TestContext.CancellationToken);
        using var metadata = new HttpRequestMessage(HttpMethod.Get, "/.well-known/oauth-authorization-server");
        metadata.Headers.Add("Origin", "vscode-file://vscode-app");
        using var get = await http.SendAsync(metadata, TestContext.CancellationToken);
        using var mcp = new HttpRequestMessage(HttpMethod.Options, "/mcp");
        mcp.Headers.Add("Origin", "vscode-file://vscode-app");
        mcp.Headers.Add("Access-Control-Request-Method", "POST");
        using var mcpOptions = await http.SendAsync(mcp, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, options.StatusCode);
        Assert.AreEqual("*", options.Headers.GetValues("Access-Control-Allow-Origin").Single());
        StringAssert.Contains(options.Headers.GetValues("Access-Control-Allow-Headers").Single(), "mcp-protocol-version");
        Assert.AreEqual(HttpStatusCode.OK, get.StatusCode);
        Assert.AreEqual("*", get.Headers.GetValues("Access-Control-Allow-Origin").Single());
        // samotny server MCP CORS nema
        Assert.IsFalse(mcpOptions.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [TestMethod]
    public async Task OAuth_VyzvaOdkazujeNaOpisZdrojaAServer()
    {
        await using var server = SimulatorServer.Start();
        using var http = server.CreateClient();

        var status = await McpStatus(http, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        using var response = await http.SendAsync(request, TestContext.CancellationToken);
        var resource = await http.GetStringAsync("/.well-known/oauth-protected-resource/mcp", TestContext.CancellationToken);
        var metadata = await http.GetStringAsync("/.well-known/oauth-authorization-server", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, status);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "resource_metadata=\"http://localhost/.well-known/oauth-protected-resource/mcp\"");
        StringAssert.Contains(resource, "\"authorization_servers\":[\"http://localhost\"]");
        StringAssert.Contains(metadata, "\"registration_endpoint\":\"http://localhost/oauth/register\"");
    }
}
