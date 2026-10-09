using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.State;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Protocols.Remote;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Linka vzdialeneho INISSu cez server: trieda 128 je medzi podporovanymi, riadiaci INISS sa pripoji po TCP, zapise
/// na tabulu Elektrocas a dostane odpoved so stavom tabule.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RemoteLineApiTests
{
    [TestMethod]
    public async Task RiadiaciIniss_ZapisNaTabuluZaVzdialenym()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;
        CollectionAssert.Contains(state.SupportedClasses, 128);
        CollectionAssert.Contains(state.Manufacturers["128"], "LCD1");

        var put = await client.PutAsJsonAsync("/api/lines/4",
            new SetLineRequest { TableClass = 128, Endpoint = $"TCP://127.0.0.1:{port}", Name = "Vzdialeny", AutoLearn = true });
        Assert.AreEqual(HttpStatusCode.NoContent, put.StatusCode, await put.Content.ReadAsStringAsync());
        for (var i = 0; i < 50 && (await client.GetFromJsonAsync<StateDto>("/api/state"))!.Lines.Single().Status != LineStatus.Listening; i++)
            await Task.Delay(100);

        using var iniss = new TcpClient();
        await iniss.ConnectAsync(IPAddress.Loopback, port);
        var stream = iniss.GetStream();
        var frame = ElektrocasFrame.Build(12, 0, 0x40, []);
        frame[^1] = 0; // riadiaci INISS sucet nevyplni
        await stream.WriteAsync(RemoteMessage.Build(RemoteMessage.Request, 42,
            [RemoteChunk.FromInt32("TCLS", 5), RemoteChunk.FromInt32("TADR", 12), new RemoteChunk("TCMD", frame)]));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var framer = new List<byte>();
        var buffer = new byte[256];
        while (framer.Count < RemoteMessage.MinLength || framer.Count < BitConverter.ToInt32(framer.ToArray(), 1))
            framer.AddRange(buffer.AsSpan(0, await stream.ReadAsync(buffer, timeout.Token)));
        var reply = RemoteMessage.Parse(framer.ToArray());

        Assert.IsTrue(reply.CrcOk);
        Assert.AreEqual(RemoteMessage.Reply, reply.Type);
        Assert.AreEqual(42u, reply.Sequence);
        Assert.AreEqual(frame.Length << 16, reply.Find("TSTA")!.Int32);

        var board = (await client.GetFromJsonAsync<StateDto>("/api/state"))!.Boards.Single();
        Assert.AreEqual("4-12", board.Id);
        Assert.AreEqual("LCD1", board.Manufacturer);
        Assert.AreEqual("chars", board.Display.Family);
    }
}
