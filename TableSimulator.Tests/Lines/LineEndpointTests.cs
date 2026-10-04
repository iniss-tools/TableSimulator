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
    [DataRow("LPT1")]
    [DataRow("COM3:abc")]
    [DataRow("TCP://host")]
    [DataRow("TCP://host:0")]
    [DataRow("")]
    public void Rozklad_ZlyTvar(string text) => Assert.IsNull(LineEndpoint.TryParse(text));

    [TestMethod]
    [DataRow("COM13", "COM13:9600,n,8,1")]
    [DataRow("com5:19200", "COM5:19200,n,8,1")]
    [DataRow("COM7:2400,E,7,2", "COM7:2400,e,7,2")]
    public void Rozklad_SeriovyPort(string text, string expected)
    {
        var endpoint = LineEndpoint.TryParse(text);

        Assert.AreEqual(LineTransport.Serial, endpoint!.Transport);
        Assert.AreEqual(expected, endpoint.ToString());
        Assert.AreEqual("", endpoint.ToInissTablePort(3));
    }

    [TestMethod]
    public void Zrkadlo_LenSeriovyPort()
    {
        var sim = new Simulator();

        Assert.ThrowsExactly<ArgumentException>(() => sim.SetLine(new LineConfig(1, 4, "TCP://0.0.0.0:47001") { Mirror = "TCP://x:1" }));
        sim.SetLine(new LineConfig(1, 4, "TCP://0.0.0.0:47001") { Mirror = "COM10" });
        Assert.AreEqual("COM10:9600,n,8,1", sim.ToConfig().Lines[0].Mirror);
    }

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
