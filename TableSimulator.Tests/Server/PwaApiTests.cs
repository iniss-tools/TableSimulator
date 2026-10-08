using System.Net;
using System.Text.Json;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Manifest aplikacie kiosku: spusti sa na stranke kiosku s klucom zariadenia na celu obrazovku, kazdy kiosk je
/// samostatna aplikacia; prehliadac ho stahuje bez prihlasenia.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PwaApiTests
{
    private const string Key = "ts_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";

    [TestMethod]
    public async Task Manifest_KioskSKlucomBezPrihlasenia()
    {
        // prihlasovanie zapnute - manifest aj tak bez cookie
        await using var server = SimulatorServer.Start("""{"requireLogin":true}""");
        var response = await server.Client().GetAsync($"/api/manifest?view=%2Fvirtual%2F3&name=Hala%20odchody&key={Key}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/manifest+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.AreEqual("/?view=%2Fvirtual%2F3", root.GetProperty("id").GetString());
        Assert.AreEqual($"/?view=%2Fvirtual%2F3&key={Key}", root.GetProperty("start_url").GetString());
        Assert.AreEqual("Hala odchody", root.GetProperty("name").GetString());
        Assert.AreEqual("Hala odchody", root.GetProperty("short_name").GetString());
        Assert.AreEqual("fullscreen", root.GetProperty("display").GetString());
        Assert.IsTrue(root.GetProperty("icons").EnumerateArray().Any(i => i.GetProperty("purpose").GetString() == "maskable"));
    }

    [TestMethod]
    [DataRow("%2Fusers", "pwa.view")]
    [DataRow("https%3A%2F%2Fexample.com", "pwa.view")]
    [DataRow("%2Fwall&key=zly", "pwa.key")]
    public async Task Manifest_LenStrankyKioskuAPlatnyKluc(string query, string code)
    {
        await using var server = SimulatorServer.Start();
        var response = await server.Client().GetAsync($"/api/manifest?view={query}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(code, json.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }
}
