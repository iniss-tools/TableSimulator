using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Settings;
using TableSimulator.Contracts.State;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Protocols.AkSignal;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Ustredna AK Signal cez server: trieda 131, server INISS2 sa pripoji po TCP a zapne automatiku, stav ustredne a jej
/// profil v API, nastavenie snimaca a stavov, chyby a poloha v nastaveni.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AkSignalApiTests
{
    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static async Task<byte[]> SendAsync(NetworkStream stream, byte[] frame)
    {
        await stream.WriteAsync(frame);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var answer = new List<byte>();
        var buffer = new byte[16];
        while (answer.Count < AkSignalFrame.AnswerLength)
            answer.AddRange(buffer.AsSpan(0, await stream.ReadAsync(buffer, timeout.Token)));
        return [.. answer];
    }

    [TestMethod]
    public async Task Ustredna_StavProfilANastavenie()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        var port = FreePort();

        var state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;
        CollectionAssert.Contains(state.SupportedClasses, 131);
        CollectionAssert.Contains(state.SupportedClasses, 132);

        var line = await client.PutAsJsonAsync("/api/lines/6",
            new SetLineRequest { TableClass = 131, Endpoint = $"TCP://127.0.0.1:{port}", Name = "Svetla", AutoLearn = false, Manufacturer = "AKSIGNAL" });
        Assert.AreEqual(HttpStatusCode.NoContent, line.StatusCode, await line.Content.ReadAsStringAsync());
        var profile = new BoardProfileDto
        {
            AkSignal = new AkSignalProfileDto { Lights = [new StationLightDto("Nástupište 1", 5400010) { BeforeArrival = 5 }], Sensor = new TwilightSensorDto(TwilightMode.Light) }
        };
        var board = await client.PutAsJsonAsync("/api/boards/6/3", new SetBoardRequest { Manufacturer = "AKSIGNAL", Name = "Nástupište 1", Profile = profile });
        Assert.AreEqual(HttpStatusCode.NoContent, board.StatusCode, await board.Content.ReadAsStringAsync());
        for (var i = 0; i < 50 && (await client.GetFromJsonAsync<StateDto>("/api/state"))!.Lines.Single().Status != LineStatus.Listening; i++)
            await Task.Delay(100);

        // server INISS2 zapne automatiku - cez den svetlo nesvieti
        using var iniss = new TcpClient();
        await iniss.ConnectAsync(IPAddress.Loopback, port);
        var stream = iniss.GetStream();
        CollectionAssert.AreEqual(AkSignalFrame.Answer(AkSignalFrame.SetLight, 0x09), await SendAsync(stream, AkSignalFrame.Build(3, AkSignalFrame.SetLight)));

        var shown = (await client.GetFromJsonAsync<BoardDto>("/api/boards/6-3"))!;
        Assert.AreEqual("aksignal", shown.Display.Family);
        Assert.IsTrue(shown.Display.AkSignal!.AutoMode);
        Assert.IsFalse(shown.Display.AkSignal.LightOn);
        Assert.AreEqual("09", shown.Display.AkSignal.LightState);
        Assert.AreEqual("Nástupište 1", shown.Profile.AkSignal!.Lights.Single().Name);

        // obsluha zapne tmu a poruchu svetiel - svetla a audio linky ostanu
        var set = await client.PutAsJsonAsync("/api/boards/6-3/aksignal",
            new AkSignalSettingsRequest(new TwilightSensorDto(TwilightMode.Dark), new AkSignalConditionsDto(LightsFault: true)));
        Assert.AreEqual(HttpStatusCode.NoContent, set.StatusCode, await set.Content.ReadAsStringAsync());
        shown = (await client.GetFromJsonAsync<BoardDto>("/api/boards/6-3"))!;
        Assert.IsTrue(shown.Display.AkSignal!.Dark, "tik po zmene prepocita snimac hned");
        Assert.IsTrue(shown.Display.AkSignal.LightOn);
        Assert.IsTrue(shown.Profile.AkSignal!.Conditions.LightsFault);
        Assert.AreEqual(1, shown.Profile.AkSignal.Lights.Count);
        CollectionAssert.AreEqual(AkSignalFrame.Answer(AkSignalFrame.GetLight, 0x05), await SendAsync(stream, AkSignalFrame.Build(3, AkSignalFrame.GetLight)));

        var invalid = await client.PutAsJsonAsync("/api/boards/6-3/aksignal",
            new AkSignalSettingsRequest(new TwilightSensorDto(TwilightMode.Sun, 999), new AkSignalConditionsDto()));
        Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        StringAssert.Contains(await invalid.Content.ReadAsStringAsync(), "board.akSignal");
        var missing = await client.PutAsJsonAsync("/api/boards/6-9/aksignal", new AkSignalSettingsRequest(new TwilightSensorDto(), new AkSignalConditionsDto()));
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [TestMethod]
    public async Task Nastavenie_PolohaSaUlozi()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();

        var settings = (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!;
        Assert.AreEqual(new GeoLocationDto(48.67, 19.70), settings.Location);

        var request = new UpdateSettingsRequest { HttpPort = settings.HttpPort, CheckUpdates = false, Location = new GeoLocationDto(48.7164, 21.2611) };
        var put = await client.PutAsJsonAsync("/api/settings", request);
        Assert.AreEqual(HttpStatusCode.OK, put.StatusCode, await put.Content.ReadAsStringAsync());
        Assert.AreEqual(new GeoLocationDto(48.7164, 21.2611), (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!.Location);

        // bez polohy v ziadosti ostane ulozena
        await client.PutAsJsonAsync("/api/settings", request with { Location = null });
        Assert.AreEqual(new GeoLocationDto(48.7164, 21.2611), (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!.Location);

        var invalid = await client.PutAsJsonAsync("/api/settings", request with { Location = new GeoLocationDto(91, 0) });
        Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
}
