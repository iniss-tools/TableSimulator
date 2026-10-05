using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TableSimulator.Contracts.Auth;
using TableSimulator.Contracts.Https;
using TableSimulator.Contracts.Settings;
using TableSimulator.Infrastructure.Https;

namespace TableSimulator.Tests.Server;

/// <summary>
/// HTTPS cez TestServer: certifikat simulatora a jeho stav, presmerovanie pristupu zo siete, vlastny certifikat
/// v nastaveniach (heslo sa nevracia ani neuklada citatelne).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HttpsApiTests
{
    private const int HttpsPort = 5999;

    private static SimulatorServer StartHttps(bool redirect = true) =>
        SimulatorServer.Start($$"""{"requireLogin":true,"httpsPort":{{HttpsPort}},"httpsRedirect":{{(redirect ? "true" : "false")}}}""");

    private static async Task<HttpClient> Admin(SimulatorServer server)
    {
        var client = server.Client();
        var setup = await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("spravca", "Správca", "heslo-spravcu"));
        Assert.AreEqual(HttpStatusCode.OK, setup.StatusCode);
        return client;
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>Poziadavka GET z adresy <paramref name="remote" /> (TestServer sam ziadnu nema).</summary>
    private static Task<HttpContext> Get(SimulatorServer server, string path, string remote) =>
        server.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Get;
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("192.168.1.20", 5470);
            context.Request.Path = path;
            context.Request.QueryString = new QueryString("?key=1");
            context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        });

    [TestMethod]
    public async Task ZoSiete_PresmerovanieNaHttpsZTohtoPocitacaNie()
    {
        await using var server = StartHttps();

        var remote = await Get(server, "/api/state", "192.168.1.30");
        var local = await Get(server, "/api/state", "127.0.0.1");
        var mapped = await Get(server, "/api/state", "::ffff:127.0.0.1");

        Assert.AreEqual(StatusCodes.Status308PermanentRedirect, remote.Response.StatusCode);
        Assert.AreEqual("https://192.168.1.20:5999/api/state?key=1", remote.Response.Headers.Location.ToString());
        Assert.AreEqual(StatusCodes.Status401Unauthorized, local.Response.StatusCode);
        Assert.AreEqual(StatusCodes.Status401Unauthorized, mapped.Response.StatusCode);
    }

    [TestMethod]
    public async Task BezPresmerovania_ZoSieteAjHttp()
    {
        await using var server = StartHttps(redirect: false);

        var remote = await Get(server, "/api/state", "192.168.1.30");

        Assert.AreEqual(StatusCodes.Status401Unauthorized, remote.Response.StatusCode);
    }

    [TestMethod]
    public async Task CertifikatSimulatora_StavAStiahnutieAutority()
    {
        await using var server = StartHttps();
        using var admin = await Admin(server);
        using var anonymous = server.Client();

        var status = await admin.GetFromJsonAsync<HttpsStatusDto>("/api/https");
        var download = await anonymous.GetAsync("/api/https/certificate");

        Assert.IsTrue(status!.Enabled);
        Assert.AreEqual(HttpsPort, status.Port);
        Assert.AreEqual(HttpsSource.Generated, status.Source);
        CollectionAssert.Contains(status.Names.ToArray(), "localhost");
        Assert.IsNull(status.Error);
        Assert.AreEqual(HttpStatusCode.OK, download.StatusCode);
        Assert.AreEqual("application/x-x509-ca-cert", download.Content.Headers.ContentType?.MediaType);
        using var authority = X509CertificateLoader.LoadCertificate(await download.Content.ReadAsByteArrayAsync());
        Assert.IsTrue(authority.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority);
        Assert.AreEqual(status.Thumbprint, LocalCertificates.Thumbprint(authority));
        Assert.IsTrue(File.Exists(Path.Combine(server.DataFolder, "https", "authority.crt")));
    }

    [TestMethod]
    public async Task BezHttps_CertifikatNieJe()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();

        var status = await client.GetFromJsonAsync<HttpsStatusDto>("/api/https");
        var download = await client.GetAsync("/api/https/certificate");

        Assert.IsFalse(status!.Enabled);
        Assert.AreEqual(HttpStatusCode.NotFound, download.StatusCode);
        Assert.AreEqual("https.disabled", await CodeOf(download));
        Assert.IsFalse(Directory.Exists(Path.Combine(server.DataFolder, "https")));
    }

    [TestMethod]
    public async Task Nastavenie_VlastnyCertifikatSHeslom()
    {
        await using var server = StartHttps();
        using var admin = await Admin(server);
        var settings = (await admin.GetFromJsonAsync<SettingsDto>("/api/settings"))!;
        var pfx = Path.Combine(server.DataFolder, "vlastny.pfx");
        using (var authority = LocalCertificates.CreateAuthority(DateTimeOffset.UtcNow))
        using (var own = LocalCertificates.CreateServer(authority, new CertificateNames(["simulator.test"], [IPAddress.Loopback]), DateTimeOffset.UtcNow))
            await File.WriteAllBytesAsync(pfx, own.Export(X509ContentType.Pkcs12, "tajne-heslo"));

        UpdateSettingsRequest Request(string certificate, string? password, int httpsPort = HttpsPort) => new()
        {
            HttpPort = settings.HttpPort, RequireLogin = true, VirtualFolders = [], HttpsPort = httpsPort, HttpsRedirect = true,
            HttpsCertificate = certificate, HttpsPassword = password
        };

        var samePort = await admin.PutAsJsonAsync("/api/settings", Request("", null, settings.HttpPort));
        var missing = await admin.PutAsJsonAsync("/api/settings", Request(pfx + ".nie", null));
        var wrong = await admin.PutAsJsonAsync("/api/settings", Request(pfx, "zle"));
        var good = await admin.PutAsJsonAsync("/api/settings", Request(pfx, "tajne-heslo"));
        // heslo sa neposiela - ostane ulozene
        var kept = await admin.PutAsJsonAsync("/api/settings", Request(pfx, null));

        Assert.AreEqual("settings.httpsPort", await CodeOf(samePort));
        Assert.AreEqual("settings.httpsCertificateMissing", await CodeOf(missing));
        Assert.AreEqual("settings.httpsCertificateInvalid", await CodeOf(wrong));
        Assert.AreEqual(HttpStatusCode.OK, good.StatusCode);
        Assert.IsFalse((await good.Content.ReadFromJsonAsync<SettingsUpdatedDto>())!.Restart);
        Assert.AreEqual(HttpStatusCode.OK, kept.StatusCode);
        var saved = await admin.GetStringAsync("/api/settings");
        Assert.Contains("\"httpsPasswordSet\":true", saved);
        Assert.DoesNotContain("tajne-heslo", saved);
        Assert.DoesNotContain("tajne-heslo", await File.ReadAllTextAsync(Path.Combine(server.DataFolder, "settings.json")));
        var status = await admin.GetFromJsonAsync<HttpsStatusDto>("/api/https");
        Assert.AreEqual(HttpsSource.File, status!.Source);
        Assert.AreEqual("CN=simulator.test, O=INISSTools", status.Subject);
    }
}
