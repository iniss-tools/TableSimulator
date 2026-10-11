using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Editor vzhladov: ulozenie vlastneho vzhladu do priecinka vzhladov (design.json s klucmi ako v dokumentacii),
/// zobrazenie bez restartu, zmazanie a overenie hodnot.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DesignsApiTests
{
    private static SetDesignRequest Design(string name = "Moj vzhľad") => new()
    {
        Name = name, Pitch = 4, Dot = 0.8, Shape = DesignShape.Square, Off = "#101010", Glow = 0.2, Background = "#000000", RowGap = 2, Padding = 1,
        Colors = new Dictionary<string, string> { ["0"] = "#ffff00", ["1"] = "#ff0000", ["2"] = "#00ff00", ["3"] = "#ffcc00", ["mono"] = "#ffff00" },
        Frame = new DesignFrameDto(6, "#222222", 4), GapColor = null,
        Header = new DesignHeaderDto("#000080", "#ffffff", "Arial", true, "#ffff00", "#000000", "Tahoma")
    };

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [TestMethod]
    public async Task Ulozenie_SuborZoznamAZmazanie()
    {
        await using var server = SimulatorServer.Start();
        var client = server.Client();

        var saved = await client.PutAsJsonAsync("/api/designs/moj-vzhlad", Design());
        Assert.AreEqual(HttpStatusCode.NoContent, saved.StatusCode);

        // design.json s klucmi ako v dokumentacii a diakritikou bez escapovania
        var file = Path.Combine(server.DataFolder, "designs", "moj-vzhlad", "design.json");
        var text = await File.ReadAllTextAsync(file);
        StringAssert.Contains(text, "\"name\": \"Moj vzhľad\"");
        StringAssert.Contains(text, "\"shape\": \"square\"");
        StringAssert.Contains(text, "\"rowGap\": 2");
        StringAssert.Contains(text, "\"gapColor\": null");
        StringAssert.Contains(text, "\"clockFont\": \"Tahoma\"");

        var list = await client.GetFromJsonAsync<DesignInfoDto[]>("/api/designs");
        var info = list!.Single(d => d.Id == "moj-vzhlad");
        Assert.IsFalse(info.BuiltIn);
        Assert.AreEqual("Moj vzhľad", info.Name);

        // priecinok vzhladov server zalozil pri starte - subor sa posiela bez restartu
        var served = await client.GetAsync("/designs/moj-vzhlad/design.json");
        Assert.AreEqual(HttpStatusCode.OK, served.StatusCode);

        Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/designs/moj-vzhlad")).StatusCode);
        Assert.IsFalse(File.Exists(file));
        var again = await client.DeleteAsync("/api/designs/moj-vzhlad");
        Assert.AreEqual(HttpStatusCode.NotFound, again.StatusCode);
        Assert.AreEqual("designs.notCustom", await CodeOf(again));
    }

    [TestMethod]
    [DataRow("Zly Vzhlad", "designs.id")]
    [DataRow("dobry", "designs.color")]
    [DataRow("prilis-velky", "designs.number")]
    public async Task Overenie_IdFarbyACisla(string id, string code)
    {
        await using var server = SimulatorServer.Start();
        var design = code switch
        {
            "designs.color" => Design() with { Background = "cierna" },
            "designs.number" => Design() with { Pitch = 500 },
            _ => Design()
        };

        var response = await server.Client().PutAsJsonAsync($"/api/designs/{Uri.EscapeDataString(id)}", design);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(code, await CodeOf(response));
    }

    [TestMethod]
    public async Task Material_PlechRamStylyAListy()
    {
        await using var server = SimulatorServer.Start();
        var client = server.Client();
        var flapColors = new[] { "black", "red", "yellow", "white" }.ToDictionary(c => c, _ => new DesignFlapColorDto("#111111", "#222222", "#ffffff"));
        var flaps = Design("Listy") with
        {
            Renderer = "flaps", Plate = "#c9cac6", Frame = new DesignFrameDto(12, "#a9adb1", 3, DesignFrameStyle.Metal, "ELEN"),
            Seam = new DesignSeamDto("#0a0f24", 0.6),
            Header = Design().Header with
            {
                Panel = "#123456", Styles = new Dictionary<string, DesignTextStyleDto> { ["label"] = new("#cccccc", null, false) }
            },
            Flaps = new DesignFlapsDto(flapColors, "#0d0d0d", "#000000", "#f2c14e", "Arial")
        };

        Assert.AreEqual(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/designs/listy", flaps)).StatusCode);
        var text = await File.ReadAllTextAsync(Path.Combine(server.DataFolder, "designs", "listy", "design.json"));
        StringAssert.Contains(text, "\"renderer\": \"flaps\"");
        StringAssert.Contains(text, "\"style\": \"metal\"");
        StringAssert.Contains(text, "\"badge\": \"ELEN\"");
        StringAssert.Contains(text, "\"plate\": \"#c9cac6\"");
        StringAssert.Contains(text, "\"split\": \"#000000\"");
        StringAssert.Contains(text, "\"label\": {");
        var info = (await client.GetFromJsonAsync<DesignInfoDto[]>("/api/designs"))!.Single(d => d.Id == "listy");
        Assert.AreEqual("flaps", info.Renderer);

        // listovy vzhlad bez listov a zly nazov stylu
        var missing = await client.PutAsJsonAsync("/api/designs/bez-listov", flaps with { Flaps = null });
        Assert.AreEqual("designs.renderer", await CodeOf(missing));
        var badStyle = await client.PutAsJsonAsync("/api/designs/zly-styl", Design() with
        {
            Header = Design().Header with { Styles = new Dictionary<string, DesignTextStyleDto> { ["Zly styl"] = new(null, null, null) } }
        });
        Assert.AreEqual("designs.font", await CodeOf(badStyle));
    }
}
