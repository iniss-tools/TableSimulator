using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        Assert.AreEqual(new VirtualBoardDto(1, "Odchody web", "Odchody – hala", "DolneMestoO", 12, 1, "", "Odjezdy", ""), boards[0]);
        Assert.AreEqual("Vlastná", boards[1].Name);

        var invalid = await client.PostAsJsonAsync("/api/virtual/import", new InissViewImportRequest("<configuration>", false));
        Assert.AreEqual("virtual.invalidConfig", await CodeOf(invalid));
    }
}
