using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TableSimulator.Contracts.Station;
using TableSimulator.Contracts.Virtual;
using static TableSimulator.Tests.Virtual.XmlTableTests;

namespace TableSimulator.Tests.Server;

/// <summary>Virtualne tabule cez API: definicia, obsah z XML v sledovanom priecinku, import z INISSView, chyby.</summary>
[TestClass]
[DoNotParallelize]
public sealed class VirtualApiTests
{
    /// <summary>Server, ktory sleduje priecinok <paramref name="logs" /> (zmaze sa so serverom).</summary>
    private static SimulatorServer Start(out string logs)
    {
        var server = SimulatorServer.Start();
        logs = Path.Combine(server.DataFolder, "Logy");
        Directory.CreateDirectory(logs);
        // nastavenie sa cita az pri prvom klientovi
        File.WriteAllText(Path.Combine(server.DataFolder, "settings.json"), JsonSerializer.Serialize(new { virtualFolders = new[] { logs } }));
        return server;
    }

    /// <summary>Kod chyby; pri neplatnych vstupoch kod prvej chyby overenia.</summary>
    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>Server prechadza priecinky kazde 2 s - caka, kym tabula dostane obsah zo suboru.</summary>
    private static async Task<VirtualViewDto> WaitForSource(HttpClient client, int id)
    {
        for (var i = 0; i < 50; i++)
        {
            var view = await client.GetFromJsonAsync<VirtualViewDto>($"/api/virtual/{id}");
            if (view!.Source is not null)
                return view;
            await Task.Delay(100);
        }

        throw new AssertFailedException("Server XML tabule nenasiel.");
    }

    [TestMethod]
    public async Task Tabula_ObsahZoSuboruPodlaDefinicie()
    {
        await using var server = Start(out var logs);
        using var client = server.Client();
        await File.WriteAllBytesAsync(Path.Combine(logs, "DolneMestoO.xml"),
            Xml(Row("11:35", "7813", "Bratislava#Wien") + Row("11:52", "601") + Row("12:10", "1771")));

        var put = await client.PutAsJsonAsync("/api/virtual/5",
            new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO", RowCount = 2, RowStart = 1, Separator = ", " });
        Assert.AreEqual(HttpStatusCode.NoContent, put.StatusCode);

        var view = await WaitForSource(client, 5);
        Assert.AreEqual("Dolné Mesto", view.Source!.Station);
        Assert.AreEqual(3, view.Source.Rows);
        Assert.HasCount(2, view.Rows);
        Assert.AreEqual("Bratislava, Wien", view.Rows[0].Cells.Single(c => c.Name == "destination").Text);

        var overview = await client.GetFromJsonAsync<VirtualOverviewDto>("/api/virtual");
        Assert.AreEqual("Odchody", overview!.Boards.Single().Name);
        Assert.AreEqual("DolneMestoO", overview.Sources.Single().Name);
        Assert.IsTrue(overview.Folders.Single().Exists);

        Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/virtual/5")).StatusCode);
        var missing = await client.GetAsync("/api/virtual/5");
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.AreEqual("virtual.notFound", await CodeOf(missing));
    }

    [TestMethod]
    public async Task Definicia_NeplatnySubor_ChybaOverenia()
    {
        await using var server = Start(out _);
        using var client = server.Client();

        var put = await client.PutAsJsonAsync("/api/virtual/1", new SetVirtualBoardRequest { Name = "Odchody", Source = @"..\iny\subor" });

        Assert.AreEqual(HttpStatusCode.BadRequest, put.StatusCode);
        Assert.AreEqual("virtual.source", await CodeOf(put));
    }

    [TestMethod]
    public async Task Import_ZWebConfig_BezPrepisaniaExistujucej()
    {
        await using var server = Start(out _);
        using var client = server.Client();
        await client.PutAsJsonAsync("/api/virtual/2", new SetVirtualBoardRequest { Name = "Vlastná", Source = "X" });
        const string webConfig = """
            <configuration><appSettings>
              <add key="TabXmlDir" value="C:\INISS\Logy" />
              <add key="1" value="Odchody web;Odchody – hala;DolneMestoO;Odjezdy;12" />
              <add key="2" value="Príchody web;;DolneMestoP" />
            </appSettings></configuration>
            """;

        var response = await client.PostAsJsonAsync("/api/virtual/import", new InissViewImportRequest(webConfig, false));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<InissViewImportedDto>();
        Assert.AreEqual(1, result!.Added);
        Assert.AreEqual(1, result.Skipped);
        CollectionAssert.AreEqual(new[] { @"C:\INISS\Logy" }, result.Folders.ToArray());
        var boards = (await client.GetFromJsonAsync<VirtualOverviewDto>("/api/virtual"))!.Boards;
        Assert.AreEqual(new VirtualBoardDto(1, "Odchody web", "Odchody – hala", "DolneMestoO", 12, 1, "", "Odjezdy", "", "odchody"), boards[0]);
        Assert.AreEqual("Vlastná", boards[1].Name);

        var invalid = await client.PostAsJsonAsync("/api/virtual/import", new InissViewImportRequest("<configuration>", false));
        Assert.AreEqual("virtual.invalidConfig", await CodeOf(invalid));
    }

    [TestMethod]
    public async Task Rozlozenie_VstavaneAVlastne()
    {
        await using var server = Start(out _);
        using var client = server.Client();
        await client.PutAsJsonAsync("/api/virtual/1", new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO" });

        var overview = (await client.GetFromJsonAsync<VirtualOverviewDto>("/api/virtual"))!;
        Assert.AreEqual("modra", overview.DefaultLayout);
        Assert.IsTrue(overview.Layouts.Single(l => l.Id == "pokladne").BuiltIn);
        Assert.AreEqual("modra", (await client.GetFromJsonAsync<VirtualViewDto>("/api/virtual/1"))!.Layout.Id);

        var mine = overview.Layouts.Single(l => l.Id == "odchody") with { Name = "Moje odchody", Rows = 12 };
        Assert.AreEqual(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/virtual/layouts/moje", mine)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync("/api/virtual/1", new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO", Layout = "moje" })).StatusCode);
        var layout = (await client.GetFromJsonAsync<VirtualViewDto>("/api/virtual/1"))!.Layout;
        Assert.AreEqual(("moje", "Moje odchody", 12, false), (layout.Id, layout.Name, layout.Rows, layout.BuiltIn));

        var inUse = await client.DeleteAsync("/api/virtual/layouts/moje");
        Assert.AreEqual(HttpStatusCode.Conflict, inUse.StatusCode);
        Assert.AreEqual("virtual.layoutInUse", await CodeOf(inUse));
        Assert.AreEqual("virtual.layoutBuiltIn", await CodeOf(await client.PutAsJsonAsync("/api/virtual/layouts/modra", mine)));
        Assert.AreEqual("virtual.layoutNotFound", await CodeOf(await client.PutAsJsonAsync("/api/virtual/1",
            new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO", Layout = "neexistuje" })));
        Assert.AreEqual("virtual.layoutColor", await CodeOf(await client.PutAsJsonAsync("/api/virtual/layouts/zle",
            mine with { Colors = mine.Colors with { Text = "white" } })));

        await client.PutAsJsonAsync("/api/virtual/1", new SetVirtualBoardRequest { Name = "Odchody", Source = "DolneMestoO" });
        Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/virtual/layouts/moje")).StatusCode);
    }

    [TestMethod]
    public async Task ImportStanice_PriecinokLogovAVirtualneTabule()
    {
        await using var server = Start(out var logs);
        using var client = server.Client();
        var other = Path.Combine(server.DataFolder, "Logy2");
        Directory.CreateDirectory(other);
        StationImportRequest Request(string folder) => new([], [])
        {
            LogFolder = folder,
            XmlTables = [new StationXmlTableDto("Odchody web", "DolneMestoO"), new StationXmlTableDto("Príchody web", "DolneMestoP")]
        };

        var first = await (await client.PostAsJsonAsync("/api/station", Request(other))).Content.ReadFromJsonAsync<StationImportedDto>();
        var again = await (await client.PostAsJsonAsync("/api/station", Request(logs + Path.DirectorySeparatorChar))).Content.ReadFromJsonAsync<StationImportedDto>();
        var missing = await (await client.PostAsJsonAsync("/api/station", Request(Path.Combine(logs, "neexistuje")))).Content
            .ReadFromJsonAsync<StationImportedDto>();

        Assert.AreEqual((2, 0, StationLogFolder.Added), (first!.VirtualBoardsAdded, first.VirtualBoardsKept, first.LogFolder));
        Assert.AreEqual((0, 2, StationLogFolder.Watched), (again!.VirtualBoardsAdded, again.VirtualBoardsKept, again.LogFolder));
        Assert.AreEqual(StationLogFolder.Missing, missing!.LogFolder);
        var overview = (await client.GetFromJsonAsync<VirtualOverviewDto>("/api/virtual"))!;
        CollectionAssert.AreEqual(new[] { (1, "DolneMestoO", "odchody"), (2, "DolneMestoP", "prichody") },
            overview.Boards.Select(b => (b.Id, b.Source, b.Layout)).ToArray());
        CollectionAssert.AreEqual(new[] { logs, other }, overview.Folders.Select(f => f.Path).ToArray());
    }
}
