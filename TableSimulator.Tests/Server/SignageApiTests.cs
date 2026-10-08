using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
}
