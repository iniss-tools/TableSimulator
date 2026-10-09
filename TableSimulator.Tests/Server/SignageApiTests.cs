using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Informacne panely cez API: sprava, playlist, skupina a panel, program panelu, ochrana pouzivanych zaznamov,
/// nahraty obrazok a jeho odstranenie so spravou.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SignageApiTests
{
    private static SetSignageMessageRequest Text(string name) =>
        new(name, "", SignageMessageKind.Text, 15, null, null, [new SignageParagraphDto("Vitajte", "#ffffff", SignageTextSize.Large, SignageAlign.Center)],
            "#000000", null, null);

    private static async Task<int> Create<T>(HttpClient client, string collection, T request)
    {
        var response = await client.PostAsJsonAsync($"/api/signage/{collection}", request);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SignageCreatedDto>())!.Id;
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [TestMethod]
    public async Task Panel_HraPlaylistSkupiny()
    {
        await using var server = SimulatorServer.Start();
        var client = server.Client();

        var first = await Create(client, "messages", Text("Uvítanie"));
        var second = await Create(client, "messages", Text("Oznam"));
        var playlist = await Create(client, "playlists",
            new SetSignagePlaylistRequest("Celý deň", SignagePeriod.Daily, new DateOnly(2020, 1, 1), null, [], 0, 0,
                [new SignagePlaylistItemDto(first, null), new SignagePlaylistItemDto(second, 5)]));
        var group = await Create(client, "groups", new SetSignageGroupRequest("Hala", "", [playlist], null, null));
        var panel = await Create(client, "panels", new SetSignagePanelRequest("Monitor 1", "pri pokladni", group));

        var program = await client.GetFromJsonAsync<SignageProgramDto>($"/api/signage/panels/{panel}/program");
        Assert.IsNull(program!.Offline);
        Assert.AreEqual(playlist, program.Playlist?.Id);
        Assert.AreEqual(2, program.Items.Count);
        Assert.AreEqual(15, program.Items[0].Duration);
        Assert.AreEqual(5, program.Items[1].Duration);
        Assert.AreEqual("Vitajte", program.Items[0].Message.Definition.Paragraphs[0].Text);

        // panel sa ohlasil a hra spravu playlistu
        var overview = await client.GetFromJsonAsync<SignageOverviewDto>("/api/signage");
        var row = overview!.Panels.Single();
        Assert.IsTrue(row.Status?.Online);
        Assert.IsTrue(row.Playing == first || row.Playing == second);

        // pouzivane zaznamy sa nedaju odstranit
        var inUse = await client.DeleteAsync($"/api/signage/messages/{first}");
        Assert.AreEqual(HttpStatusCode.Conflict, inUse.StatusCode);
        Assert.AreEqual("signage.messageInUse", await CodeOf(inUse));
        Assert.AreEqual("signage.groupInUse", await CodeOf(await client.DeleteAsync($"/api/signage/groups/{group}")));

        // panel bez skupiny je mimo prevadzky
        Assert.AreEqual(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/signage/panels/{panel}", new SetSignagePanelRequest("Monitor 1", "", null))).StatusCode);
        program = await client.GetFromJsonAsync<SignageProgramDto>($"/api/signage/panels/{panel}/program");
        Assert.AreEqual(SignageOffline.NoGroup, program!.Offline);
        Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/signage/groups/{group}")).StatusCode);

        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/signage/panels/99/program")).StatusCode);
    }

    [TestMethod]
    public async Task Overenie_OdkazyAVstupy()
    {
        await using var server = SimulatorServer.Start();
        var client = server.Client();

        var bad = await client.PostAsJsonAsync("/api/signage/messages", Text("Zlá farba") with { Background = "cierna" });
        Assert.AreEqual("signage.color", await CodeOf(bad));
        var noVirtual = await client.PostAsJsonAsync("/api/signage/messages", Text("P/O") with { Kind = SignageMessageKind.Virtual, VirtualBoard = 5 });
        Assert.AreEqual("signage.unknownVirtual", await CodeOf(noVirtual));
        var noMessage = await client.PostAsJsonAsync("/api/signage/playlists",
            new SetSignagePlaylistRequest("P", SignagePeriod.Once, new DateOnly(2026, 1, 1), null, [], 0, 60, [new SignagePlaylistItemDto(42, null)]));
        Assert.AreEqual("signage.unknownMessage", await CodeOf(noMessage));
        var weekly = await client.PostAsJsonAsync("/api/signage/playlists",
            new SetSignagePlaylistRequest("T", SignagePeriod.Weekly, new DateOnly(2026, 1, 1), null, [], 0, 60, []));
        Assert.AreEqual("signage.weekdays", await CodeOf(weekly));
        var noGroup = await client.PostAsJsonAsync("/api/signage/panels", new SetSignagePanelRequest("M", "", 3));
        Assert.AreEqual("signage.unknownGroup", await CodeOf(noGroup));
    }

    [TestMethod]
    public async Task Obrazok_NahratieZobrazenieAOdstranenieSoSpravou()
    {
        await using var server = SimulatorServer.Start();
        var client = server.Client();
        byte[] png = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 1, 2, 3];

        var wrongType = await client.PostAsync("/api/signage/media?name=obrazok.svg", new ByteArrayContent(png));
        Assert.AreEqual("signage.mediaType", await CodeOf(wrongType));

        var content = new ByteArrayContent(png);
        // typ urcuje pripona mena - telo ide ako application/octet-stream (ako z webu)
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var uploaded = await client.PostAsync("/api/signage/media?name=plagát.png", content);
        Assert.AreEqual(HttpStatusCode.OK, uploaded.StatusCode, await uploaded.Content.ReadAsStringAsync());
        var media = (await uploaded.Content.ReadFromJsonAsync<SignageMediaDto>())!;
        Assert.AreEqual("image/png", media.ContentType);
        Assert.AreEqual(png.Length, media.Size);

        var served = await client.GetAsync($"/api/signage/media/{media.Id}");
        Assert.AreEqual(HttpStatusCode.OK, served.StatusCode);
        Assert.AreEqual("image/png", served.Content.Headers.ContentType?.MediaType);
        CollectionAssert.AreEqual(png, await served.Content.ReadAsByteArrayAsync());

        // video sprava s obrazkom nejde
        var request = Text("Plagát") with { Kind = SignageMessageKind.Video, Media = media.Id, Paragraphs = [] };
        Assert.AreEqual("signage.mediaKind", await CodeOf(await client.PostAsJsonAsync("/api/signage/messages", request)));
        var message = await Create(client, "messages", request with { Kind = SignageMessageKind.Image });
        var overview = await client.GetFromJsonAsync<SignageOverviewDto>("/api/signage");
        Assert.AreEqual("plagát.png", overview!.Messages.Single().MediaName);

        Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/signage/messages/{message}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync($"/api/signage/media/{media.Id}")).StatusCode);
        Assert.IsEmpty(Directory.GetFiles(Path.Combine(server.DataFolder, "media")));
    }

    /// <summary>Sprava s obrazkom, playlist, skupina a panel cislo 1 na serveri; vrati cislo spravy s obrazkom.</summary>
    private static async Task<(int Message, Guid Media)> Seed(HttpClient client)
    {
        var content = new ByteArrayContent([0x89, 0x50, 0x4e, 0x47, 1, 2, 3]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var media = (await (await client.PostAsync("/api/signage/media?name=plagat.png", content)).Content.ReadFromJsonAsync<SignageMediaDto>())!;
        var text = await Create(client, "messages", Text("Uvítanie"));
        var image = await Create(client, "messages", Text("Plagát") with { Kind = SignageMessageKind.Image, Media = media.Id, Paragraphs = [] });
        var playlist = await Create(client, "playlists",
            new SetSignagePlaylistRequest("Deň", SignagePeriod.Daily, new DateOnly(2020, 1, 1), null, [], 0, 0,
                [new SignagePlaylistItemDto(text, null), new SignagePlaylistItemDto(image, 5)]));
        var group = await Create(client, "groups", new SetSignageGroupRequest("Hala", "", [playlist], null, text));
        await Create(client, "panels", new SetSignagePanelRequest("Monitor", "", group));
        return (image, media.Id);
    }

    private static async Task<HttpResponseMessage> Import(HttpClient client, byte[] zip, bool replace) =>
        await client.PostAsync($"/api/signage/import?replace={(replace ? "true" : "false")}", new ByteArrayContent(zip));

    [TestMethod]
    public async Task Zaloha_ObnovaNaInomServeriSCislamiASubormi()
    {
        byte[] zip;
        SignageOverviewDto original;
        Guid media;
        await using (var source = SimulatorServer.Start())
        {
            var client = source.Client();
            (_, media) = await Seed(client);
            original = (await client.GetFromJsonAsync<SignageOverviewDto>("/api/signage"))!;
            var export = await client.GetAsync("/api/signage/export");
            Assert.AreEqual("application/zip", export.Content.Headers.ContentType?.MediaType);
            StringAssert.StartsWith(export.Content.Headers.ContentDisposition?.FileName, "informacne-panely-");
            zip = await export.Content.ReadAsByteArrayAsync();
        }

        await using var target = SimulatorServer.Start();
        var other = target.Client();
        var imported = (await (await Import(other, zip, replace: true)).Content.ReadFromJsonAsync<SignageImportedDto>())!;
        Assert.AreEqual((2, 1, 1, 1, 1, true), (imported.Messages, imported.Playlists, imported.Groups, imported.Panels, imported.Media, imported.Replaced));

        var restored = (await other.GetFromJsonAsync<SignageOverviewDto>("/api/signage"))!;
        // cisla ostali - monitor s #/panel/1 ide dalej
        Assert.AreEqual(JsonSerializer.Serialize(original.Messages), JsonSerializer.Serialize(restored.Messages));
        Assert.AreEqual(JsonSerializer.Serialize(original.Groups), JsonSerializer.Serialize(restored.Groups));
        Assert.AreEqual(original.Panels.Single().Id, restored.Panels.Single().Id);
        Assert.AreEqual(HttpStatusCode.OK, (await other.GetAsync($"/api/signage/media/{media}")).StatusCode);
    }

    [TestMethod]
    public async Task Zaloha_PridanieSNovymiCislamiAChybneZalohy()
    {
        await using var server = SimulatorServer.Start();
        var client = server.Client();
        var (image, _) = await Seed(client);
        var zip = await (await client.GetAsync("/api/signage/export")).Content.ReadAsByteArrayAsync();

        var added = (await (await Import(client, zip, replace: false)).Content.ReadFromJsonAsync<SignageImportedDto>())!;
        Assert.IsFalse(added.Replaced);
        // subor s rovnakym cislom uz je
        Assert.AreEqual(0, added.Media);

        var overview = (await client.GetFromJsonAsync<SignageOverviewDto>("/api/signage"))!;
        Assert.HasCount(4, overview.Messages);
        Assert.HasCount(2, overview.Panels);
        var copy = overview.Playlists.Single(p => p.Id != overview.Playlists.Min(x => x.Id));
        CollectionAssert.AreEqual(new[] { image + 1, image + 2 }, copy.Definition.Items.Select(i => i.Message).ToArray());
        var copyGroup = overview.Groups.Single(g => g.Definition.Playlists.Contains(copy.Id));
        // novy panel patri do novej skupiny
        Assert.AreEqual(copyGroup.Id, overview.Panels.MaxBy(p => p.Id)!.Definition.Group);

        Assert.AreEqual("signage.importFormat", await CodeOf(await Import(client, Encoding.UTF8.GetBytes("nie je ZIP"), replace: true)));

        // playlist so spravou, ktora v zalohe nie je
        using var broken = new MemoryStream();
        using (var archive = new ZipArchive(broken, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("signage.json");
            await using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync("""
                {"format":"TableSimulator.Signage","version":1,"exported":"2026-10-09T12:00:00+02:00","messages":[],
                 "playlists":[{"id":1,"definition":{"name":"P","period":"daily","start":"2026-01-01","end":null,"weekdays":[],"from":0,"to":0,
                 "items":[{"message":7,"duration":null}]}}],"groups":[],"panels":[],"media":[]}
                """);
        }

        Assert.AreEqual("signage.importInvalid", await CodeOf(await Import(client, broken.ToArray(), replace: true)));
        Assert.HasCount(4, (await client.GetFromJsonAsync<SignageOverviewDto>("/api/signage"))!.Messages);
    }
}
