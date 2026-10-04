using System.Net;
using System.Net.Sockets;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Lines;

/// <summary>
/// Koncovy bod linky a sietovy server.
/// </summary>
[TestClass]
public class LineEndpointTests
{
    [TestMethod]
    [DataRow("TCP://0.0.0.0:47003", LineTransport.Tcp, "0.0.0.0", 47003)]
    [DataRow("udp://10.0.0.5:4001", LineTransport.Udp, "10.0.0.5", 4001)]
    public void Rozklad(string text, LineTransport transport, string host, int port)
    {
        Assert.AreEqual(new LineEndpoint(transport, host, port), LineEndpoint.TryParse(text));
    }

    [TestMethod]
    [DataRow("COM3")]
    [DataRow("TCP://host")]
    [DataRow("TCP://host:0")]
    [DataRow("")]
    public void Rozklad_ZlyTvar(string text) => Assert.IsNull(LineEndpoint.TryParse(text));

    [TestMethod]
    public void TablePortPreIniss()
    {
        Assert.AreEqual("3=TCP://127.0.0.1:47003", LineEndpoint.DefaultFor(3).ToInissTablePort(3));
        Assert.AreEqual("3=TCP://192.168.1.5:47003", LineEndpoint.DefaultFor(3).ToInissTablePort(3, "192.168.1.5"));
    }

    [TestMethod]
    public async Task Server_PotvrdiSpravuCezTcp()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var sim = new Simulator();
        sim.SetLine(new LineConfig(2, 4, $"TCP://127.0.0.1:{port}"));
        await using var transport = new TransportManager(sim);
        await transport.SyncAsync();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();
        await stream.WriteAsync(ElenFrame.Build(5, 1, Hex("81 10 80 41")));
        var reply = new byte[1];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var read = await stream.ReadAsync(reply, timeout.Token);

        Assert.AreEqual(1, read);
        Assert.AreEqual(0x06, reply[0]);
        Assert.AreEqual(LineStatus.Connected, sim.Read(s => s.FindLine(2))!.Status);
    }
}
