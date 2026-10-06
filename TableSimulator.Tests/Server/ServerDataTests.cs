using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Config;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Settings;
using TableSimulator.Contracts.State;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Udaje servera v databaze: nastavenie, linky a tabule prezivu restart, stare subory sa prevedu raz, export a import
/// liniek a tabul.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ServerDataTests
{
    /// <summary>Linka s rurou - simulator sa k nej len pokusa pripojit, port neobsadi.</summary>
    private static SetLineRequest Pipe(string name) => new() { TableClass = 4, Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N"), Name = name };

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [TestMethod]
    public async Task Restart_NastavenieLinkyATabuleZDatabazy()
    {
        string folder;
        await using (var server = SimulatorServer.Start("""{"fontsFolder":"C:\\Pisma","virtualFolders":["C:\\Logy"]}""", deleteFolder: false))
        {
            folder = server.DataFolder;
            using var client = server.Client();
            var settings = (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!;
            Assert.AreEqual(@"C:\Pisma", settings.FontsFolder);
            CollectionAssert.AreEqual(new[] { @"C:\Logy" }, settings.VirtualFolders.ToArray());
            Assert.AreEqual(Path.Combine(folder, "tablesimulator.db"), settings.DatabasePath);

            Assert.AreEqual(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/lines/5", Pipe("Nástupištia"))).StatusCode);
            Assert.AreEqual(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/boards/5/3", new SetBoardRequest
            {
                Manufacturer = "ELEN", Name = "Nástupište 1", Profile = new BoardProfileDto { Rows = 3 }, Fault = new BoardFaultDto(FaultKind.Delay, 250)
            })).StatusCode);
            Assert.IsTrue((await client.PutAsJsonAsync("/api/settings", new UpdateSettingsRequest
            {
                HttpPort = settings.HttpPort, FontsFolder = @"D:\Pisma", VirtualFolders = settings.VirtualFolders
            })).IsSuccessStatusCode);
        }

        // stare subory sa previedli raz a ostali ako *.imported
        Assert.IsFalse(File.Exists(Path.Combine(folder, "settings.json")));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "settings.json.imported")));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "config.json.imported")));

        await using (var server = SimulatorServer.Open(folder, true))
        {
            using var client = server.Client();
            var state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;

            Assert.AreEqual(@"D:\Pisma", state.Settings.FontsFolder);
            Assert.AreEqual("Nástupištia", state.Lines.Single().Name);
            var board = state.Boards.Single();
            Assert.AreEqual("5-3", board.Id);
            Assert.AreEqual("Nástupište 1", board.Name);
            Assert.AreEqual(3, board.Profile.Rows);
            Assert.AreEqual(new BoardFaultDto(FaultKind.Delay, 250), board.Fault);
        }
    }

    [TestMethod]
    public async Task ExportImport_PridatChybajuceANahraditVsetko()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        await client.PutAsJsonAsync("/api/lines/5", Pipe("Päť"));
        await client.PutAsJsonAsync("/api/boards/5/3", new SetBoardRequest { Manufacturer = "ELEN", Name = "Tabuľa" });

        var export = await client.GetAsync("/api/config");
        Assert.AreEqual("application/json", export.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("tablesimulator-linky.json", export.Content.Headers.ContentDisposition?.FileNameStar ?? export.Content.Headers.ContentDisposition?.FileName);
        var exported = await export.Content.ReadAsStringAsync();
        StringAssert.Contains(exported, "\"Tabuľa\"");

        var other = """{"lines":[{"number":9,"tableClass":4,"endpoint":"\\\\.\\PIPE\\TableSimulatorTest-import","name":"Deväť","mode":"monitor"}],"boards":[]}""";
        var replaced = await client.PostAsJsonAsync("/api/config/import", new ImportConfigRequest(other, true));
        Assert.AreEqual(new ConfigImportedDto(1, 0, true), await replaced.Content.ReadFromJsonAsync<ConfigImportedDto>());
        var state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;
        Assert.AreEqual(9, state.Lines.Single().Number);
        Assert.AreEqual(LineMode.Monitor, state.Lines.Single().Mode);
        Assert.IsEmpty(state.Boards);

        var added = await client.PostAsJsonAsync("/api/config/import", new ImportConfigRequest(exported, false));
        Assert.AreEqual(new ConfigImportedDto(1, 1, false), await added.Content.ReadFromJsonAsync<ConfigImportedDto>());
        state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;
        CollectionAssert.AreEqual(new[] { 5, 9 }, state.Lines.Select(l => l.Number).ToArray());
        Assert.AreEqual("Tabuľa", state.Boards.Single().Name);
    }

    [TestMethod]
    public async Task Import_NeplatnySuborNicNezmeni()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        await client.PutAsJsonAsync("/api/lines/5", Pipe("Päť"));

        var notJson = await client.PostAsJsonAsync("/api/config/import", new ImportConfigRequest("<config/>", true));
        var boardWithoutLine = await client.PostAsJsonAsync("/api/config/import",
            new ImportConfigRequest("""{"lines":[],"boards":[{"line":7,"address":1,"manufacturer":"ELEN"}]}""", true));
        var empty = await client.PostAsJsonAsync("/api/config/import", new ImportConfigRequest("", false));

        Assert.AreEqual("config.invalid", await CodeOf(notJson));
        Assert.AreEqual("config.invalid", await CodeOf(boardWithoutLine));
        Assert.AreEqual("config.content", await CodeOf(empty));
        Assert.AreEqual(5, (await client.GetFromJsonAsync<StateDto>("/api/state"))!.Lines.Single().Number);
    }
}
