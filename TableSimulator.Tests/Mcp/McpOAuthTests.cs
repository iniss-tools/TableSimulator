using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Auth;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Persistence.Identity;
using TableSimulator.Tests.Server;

namespace TableSimulator.Tests.Mcp;

/// <summary>Prihlasenie klienta MCP cez OAuth: registracia, povolenie s rolou, tokeny a nastroje podla roly.</summary>
[TestClass]
[DoNotParallelize]
public sealed partial class McpOAuthTests
{
    public TestContext TestContext { get; set; } = null!;

    [GeneratedRegex("name=\"request\" value=\"([^\"]+)\"")]
    private static partial Regex RequestField();

    [TestMethod]
    public async Task OAuth_KlientSdkSaPrihlasiSRolouZPovolenia()
    {
        await using var server = SimulatorServer.Start();
        // prehliadac pouzivatela - bez prihlasovania je spravca; presmerovanie spat ku klientovi necha na test
        using var browser = server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.Server.BaseAddress, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            OAuth = new ClientOAuthOptions
            {
                RedirectUri = new Uri("http://127.0.0.1:47123/callback"),
                DynamicClientRegistration = new DynamicClientRegistrationOptions { ClientName = "Skusobny agent" },
                AuthorizationCallbackHandler = async (callback, cancellationToken) =>
                {
                    var page = await browser.GetStringAsync(callback.AuthorizationUri, cancellationToken);
                    StringAssert.Contains(page, "Skusobny agent");
                    using var form = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["request"] = RequestField().Match(page).Groups[1].Value,
                        ["decision"] = "allow",
                        ["role"] = "viewer"
                    });
                    using var response = await browser.PostAsync("/oauth/authorize", form, cancellationToken);
                    Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
                    var back = response.Headers.Location!;
                    StringAssert.StartsWith(back.ToString(), "http://127.0.0.1:47123/callback?code=");
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

    /// <summary>Overovaci kod PKCE a jeho vyzva z RFC 7636.</summary>
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
    private const string Redirect = "http://127.0.0.1:47123/callback";

    /// <summary>Registracia, povolenie s rolou a vymena kodu - odpoved /oauth/token a client_id.</summary>
    private async Task<(JsonElement Tokens, string ClientId)> Authorize(HttpClient browser, string role)
    {
        var token = TestContext.CancellationToken;
        using var registered = await browser.PostAsJsonAsync("/oauth/register", new { client_name = "Agent", redirect_uris = new[] { Redirect } }, token);
        var clientId = (await registered.Content.ReadFromJsonAsync<JsonElement>(token)).GetProperty("client_id").GetString()!;
        var page = await browser.GetStringAsync(
            $"/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(Redirect)}&code_challenge={Challenge}&code_challenge_method=S256&state=s",
            token);
        using var decision = await browser.PostAsync("/oauth/authorize", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["request"] = RequestField().Match(page).Groups[1].Value,
            ["decision"] = "allow",
            ["role"] = role
        }), token);
        var code = System.Web.HttpUtility.ParseQueryString(decision.Headers.Location!.Query)["code"]!;
        var tokens = await Token(browser, new() { ["grant_type"] = "authorization_code", ["code"] = code, ["client_id"] = clientId, ["redirect_uri"] = Redirect, ["code_verifier"] = Verifier });
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
    public async Task OAuth_ObnovenieVymeniTokenyAStaryTokenPovolenieZrusi()
    {
        await using var server = SimulatorServer.Start();
        using var browser = server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (first, clientId) = await Authorize(browser, "viewer");
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
        using var browser = server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var access = (await Authorize(browser, "viewer")).Tokens.GetProperty("access_token").GetString();

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
        using var browser = server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var registered = await browser.PostAsJsonAsync("/oauth/register", new { client_name = "Agent", redirect_uris = new[] { "http://example.com/callback" } },
            TestContext.CancellationToken);
        var foreign = await browser.GetAsync("/oauth/authorize?response_type=code&client_id=nieje&redirect_uri=https%3A%2F%2Fevil.example&code_challenge=x&code_challenge_method=S256",
            TestContext.CancellationToken);
        var badVerifier = await Token(browser, new() { ["grant_type"] = "authorization_code", ["code"] = "x", ["client_id"] = "y", ["redirect_uri"] = Redirect, ["code_verifier"] = "z" });

        // http mimo tohto pocitaca sa ako navrat zaregistrovat neda
        Assert.AreEqual(HttpStatusCode.BadRequest, registered.StatusCode);
        // neznamy klient - ziadne presmerovanie na cudziu adresu
        Assert.AreEqual(HttpStatusCode.BadRequest, foreign.StatusCode);
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
        using var browser = server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        browser.DefaultRequestHeaders.Add(CsrfHeader.HeaderName, "TableSimulator");
        using var registered = await browser.PostAsJsonAsync("/oauth/register", new { client_name = "Agent", redirect_uris = new[] { Redirect } }, TestContext.CancellationToken);
        var clientId = (await registered.Content.ReadFromJsonAsync<JsonElement>(TestContext.CancellationToken)).GetProperty("client_id").GetString();
        var authorize = $"/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(Redirect)}&code_challenge={Challenge}&code_challenge_method=S256";
        var anonymous = await browser.GetAsync(authorize, TestContext.CancellationToken);
        using var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("dispecer", "heslo-dispecera", false), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);

        // stranka ponukne len roly, ktore pouzivatel pokryva; vyssiu server odmietne
        var page = await browser.GetStringAsync(authorize, TestContext.CancellationToken);
        using var admin = await browser.PostAsync("/oauth/authorize", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["request"] = RequestField().Match(page).Groups[1].Value,
            ["decision"] = "allow",
            ["role"] = Roles.Admin
        }), TestContext.CancellationToken);
        var (tokens, grantClient) = await Authorize(browser, Roles.Viewer);
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByNameAsync("dispecer"))!;
            user.IsDisabled = true;
            await users.UpdateAsync(user);
        }
        var refreshed = await Token(browser, new() { ["grant_type"] = "refresh_token", ["refresh_token"] = tokens.GetProperty("refresh_token").GetString()!, ["client_id"] = grantClient });

        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        StringAssert.Contains(page, "value=\"operator\"");
        Assert.IsFalse(page.Contains("value=\"admin\"", StringComparison.Ordinal));
        Assert.AreEqual(HttpStatusCode.BadRequest, admin.StatusCode);
        Assert.AreEqual("invalid_grant", refreshed.Body.GetProperty("error").GetString());
    }

    [TestMethod]
    public async Task OAuth_PovoleniePrezijeRestartServera()
    {
        string access;
        string folder;
        await using (var server = SimulatorServer.Start(deleteFolder: false))
        {
            folder = server.DataFolder;
            using var browser = server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            access = (await Authorize(browser, "operator")).Tokens.GetProperty("access_token").GetString()!;
        }

        await using var restarted = SimulatorServer.Open(folder, deleteFolder: true);
        using var http = restarted.CreateClient();

        Assert.AreEqual(HttpStatusCode.OK, await McpStatus(http, access));
    }

    [TestMethod]
    public async Task OAuth_VyzvaOdkazujeNaOpisZdrojaAServer()
    {
        await using var server = SimulatorServer.Start();
        using var http = server.CreateClient();

        using var initialize = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}""",
            System.Text.Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = initialize };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        using var response = await http.SendAsync(request, TestContext.CancellationToken);
        var challenge = response.Headers.WwwAuthenticate.ToString();
        var resource = await http.GetStringAsync("/.well-known/oauth-protected-resource/mcp", TestContext.CancellationToken);
        var metadata = await http.GetStringAsync("/.well-known/oauth-authorization-server", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(challenge, "resource_metadata=\"http://localhost/.well-known/oauth-protected-resource/mcp\"");
        StringAssert.Contains(resource, "\"authorization_servers\":[\"http://localhost\"]");
        StringAssert.Contains(metadata, "\"registration_endpoint\":\"http://localhost/oauth/register\"");
    }
}
