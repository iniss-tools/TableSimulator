using System.IO.Pipes;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Lines;

/// <summary>
/// Linka cez pomenovanu ruru - ruru vytvara INISS (server), simulator sa k nej pripaja.
/// </summary>
[TestClass]
public class PipeLineTests
{
    [TestMethod]
    [DataRow(@"\\.\PIPE\TableSim3", ".", "TableSim3")]
    [DataRow(@"\\127.0.0.1\pipe\INISS3", "127.0.0.1", "INISS3")]
    public void Rozklad_Rura(string text, string host, string pipe)
    {
        var endpoint = LineEndpoint.TryParse(text);

        Assert.AreEqual(new LineEndpoint(LineTransport.Pipe, host, 0, pipe), endpoint);
        Assert.AreEqual(text.Replace(@"\pipe\", @"\PIPE\", StringComparison.Ordinal), endpoint!.ToString());
    }

    [TestMethod]
    [DataRow(@"\\.\MAILSLOT\x")]
    [DataRow(@"\\.\PIPE\")]
    [DataRow(@"\\\PIPE\x")]
    public void Rozklad_ZlaRura(string text) => Assert.IsNull(LineEndpoint.TryParse(text));

    [TestMethod]
    public void TablePortPreIniss_RuraVzdyMiestna()
    {
        Assert.AreEqual(@"3=\\.\PIPE\TableSim3", LineEndpoint.TryParse(@"\\pc-iniss\PIPE\TableSim3")!.ToInissTablePort(3));
    }

    [TestMethod]
    public async Task Rura_PripojiSaAPotvrdi()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Pomenovane rury v rezime sprav su len vo Windows.");
            return;
        }

        var name = "TableSimTest-" + Guid.NewGuid().ToString("N");
        // ako INISS: obojsmerna rura v rezime sprav
        await using var iniss = new NamedPipeServerStream(name, PipeDirection.InOut, 5, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

        var sim = new Simulator();
        sim.SetLine(new LineConfig(3, 4, $@"\\.\PIPE\{name}"));
        await using var transport = new TransportManager(sim);
        await transport.SyncAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await iniss.WaitForConnectionAsync(timeout.Token);
        await iniss.WriteAsync(ElenFrame.Build(5, 1, Hex("81 10 80 41")), timeout.Token);
        var reply = new byte[16];
        var read = await iniss.ReadAsync(reply, timeout.Token);

        Assert.AreEqual(1, read);
        Assert.AreEqual(0x06, reply[0]);
        Assert.AreEqual(LineStatus.Connected, sim.Read(s => s.FindLine(3))!.Status);
    }
}
