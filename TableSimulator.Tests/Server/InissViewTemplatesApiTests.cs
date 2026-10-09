using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TableSimulator.Contracts.Virtual;
using ToolsCore.Iniss.Tools;
using static TableSimulator.Tests.Virtual.XmlTableTests;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Virtualne tabule so sablonou XSL z priecinka INISSView: stranka v obsahu tabule, chybajuca sablona, subory stranky
/// pod /inissview/ bez prihlasenia (bez nastavenia aplikacie) a nahrada XMLStatus.aspx.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class InissViewTemplatesApiTests
{
    private string _root = "";

    [TestInitialize]
    public void Init() => _root = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private const string Template = """
        <?xml version="1.0" encoding="windows-1250"?>
        <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
        <xsl:output method="html" encoding="windows-1250"/>
        <xsl:template match="/station">
        <html><head><meta http-equiv="Refresh" content="30"/><link rel="stylesheet" href="tabs/odchody.css"/><script src="JScript.js"></script></head>
        <body onload="initTabForm();"><xsl:for-each select="row"><p><xsl:value-of select="destination"/></p></xsl:for-each></body></html>
        </xsl:template>
        </xsl:stylesheet>
        """;

    /// <summary>Priecinok INISSView so sablonou, CSS, skriptom a Web.config; priecinok logov s XML tabule.</summary>
    private (string InissView, string Logs) Folders()
    {
        var inissView = Path.Combine(_root, "INISSview");
        var logs = Path.Combine(_root, "Logy");
        Directory.CreateDirectory(Path.Combine(inissView, "tabs"));
        Directory.CreateDirectory(logs);
        File.WriteAllBytes(Path.Combine(inissView, "tabs", "Odchody.xsl"), Encodings.Win1250.GetBytes(Template));
        File.WriteAllText(Path.Combine(inissView, "tabs", "odchody.css"), "body { background: black; }");
        File.WriteAllText(Path.Combine(inissView, "JScript.js"), "function initTabForm() {}");
        File.WriteAllText(Path.Combine(inissView, "Web.config"), """
            <?xml version="1.0"?>
            <configuration><appSettings><add key="Jscript.iNextString" value="1500" /></appSettings></configuration>
            """);
        File.WriteAllBytes(Path.Combine(logs, "DolneMestoO.xml"), Xml(Row("11:35", "7813", "Bratislava#Wien") + Row("11:52", "601")));
        return (inissView, logs);
    }

    private static string Settings(string inissView, string logs, bool requireLogin = false) =>
        JsonSerializer.Serialize(new { inissViewFolder = inissView, virtualFolders = new[] { logs }, requireLogin });

    [TestMethod]
    public async Task Tabula_StrankaZoSablony()
    {
        var (inissView, logs) = Folders();
        await using var server = SimulatorServer.Start(Settings(inissView, logs));
        using var client = server.Client();

        await client.PutAsJsonAsync("/api/virtual/5", new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO", Separator = ", ", Template = "Odchody" });
        var view = await WaitForPage(client, 5);
        StringAssert.Contains(view.Html, "<base href=\"/inissview/app/\">");
        StringAssert.Contains(view.Html, "<p>Bratislava, Wien</p>");
        StringAssert.Contains(view.Html, "iNextString=1500;");
        // stranku obnovuje simulator, nie prehliadac
        Assert.DoesNotContain("Refresh", view.Html!);

        var overview = await client.GetFromJsonAsync<VirtualOverviewDto>("/api/virtual");
        CollectionAssert.AreEqual(new[] { "Odchody" }, overview!.Templates.ToArray());

        await client.PutAsJsonAsync("/api/virtual/5", new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO", Template = "Neexistuje" });
        var missing = await client.GetFromJsonAsync<VirtualViewDto>("/api/virtual/5");
        Assert.AreEqual("notFound", missing!.Page?.Error);

        // bez sablony podla rozlozenia
        await client.PutAsJsonAsync("/api/virtual/5", new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO" });
        Assert.IsNull((await client.GetFromJsonAsync<VirtualViewDto>("/api/virtual/5"))!.Page);
    }

    [TestMethod]
    public async Task SuboryStranky_BezPrihlaseniaLenVzhlad()
    {
        var (inissView, logs) = Folders();
        await using var server = SimulatorServer.Start(Settings(inissView, logs, requireLogin: true));
        using var client = server.Client();

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/virtual")).StatusCode);
        var css = await client.GetAsync("/inissview/app/tabs/odchody.css");
        Assert.AreEqual(HttpStatusCode.OK, css.StatusCode);
        Assert.AreEqual("text/css", css.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/inissview/app/JScript.js")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/inissview/app/Web.config")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/inissview/app/tabs/Odchody.xsl")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/inissview/app/..%2F..%2FLogy%2FDolneMestoO.xml")).StatusCode);

        var status = await client.GetStringAsync("/inissview/app/XMLStatus.aspx?t=5&x=1");
        StringAssert.Matches(status, new System.Text.RegularExpressions.Regex(@"^<!--ACTTIME\d{14}--><!--TableSimulator-->$"));
    }

    /// <summary>Server prechadza priecinky kazde 2 s - caka, kym tabula dostane stranku zo suboru.</summary>
    private static async Task<VirtualPageDto> WaitForPage(HttpClient client, int id)
    {
        for (var i = 0; i < 50; i++)
        {
            var view = await client.GetFromJsonAsync<VirtualViewDto>($"/api/virtual/{id}");
            if (view!.Page?.Html is not null)
                return view.Page;
            await Task.Delay(100);
        }

        throw new AssertFailedException("Stranka zo sablony nevznikla.");
    }
}
