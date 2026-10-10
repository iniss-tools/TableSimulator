using System.Net;
using System.Net.Sockets;
using TableSimulator.Application.Live;
using TableSimulator.Application.Scenarios;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Scenarios;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Scenarios;

/// <summary>
/// Scenare porucha v case: casova os krokov, vypadok napajania tabule, vypadok linky a spustac, ktory poruchy po skonceni
/// vrati.
/// </summary>
[TestClass]
public sealed class FaultScenarioTests
{
    private sealed class TestLive : ILiveNotifier
    {
        public int Count { get; private set; }

        public void Publish(LiveMessage message)
        {
            if (message is ScenariosMessage)
                Count++;
        }
    }

    private static readonly BoardFault Nak = new(FaultKind.Nak);
    private static readonly BoardFault NoReply = new(FaultKind.NoReply);

    private static FaultScenario Scenario(bool loop, params ScenarioStep[] steps) => new("Test", loop, null, steps);

    private static ScenarioStep Fault(int at, int duration, int line, int? address, BoardFault fault) =>
        new(at, duration, line, address, ScenarioAction.Fault) { Fault = fault };

    private static Simulator Station(TimeProvider? time = null)
    {
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47101") { AutoLearn = false, Manufacturer = "ELEN10" });
        sim.SetBoard(new BoardConfig(1, 5, "ELEN10"));
        sim.SetBoard(new BoardConfig(1, 6, "ELEN10") { Fault = new BoardFault(FaultKind.Delay, 300) });
        return sim;
    }

    [TestMethod]
    public void CasovaOs_KrokyRazAjDookola()
    {
        var once = Scenario(false, Fault(1000, 2000, 1, 5, Nak), Fault(2500, 1000, 1, 5, NoReply));

        Assert.AreEqual(3500, once.LengthMs);
        CollectionAssert.AreEqual(Array.Empty<int>(), ScenarioTimeline.At(once, TimeSpan.FromMilliseconds(999)).ActiveSteps.ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1 }, ScenarioTimeline.At(once, TimeSpan.FromMilliseconds(2600)).ActiveSteps.ToArray());
        CollectionAssert.AreEqual(new[] { 1 }, ScenarioTimeline.At(once, TimeSpan.FromMilliseconds(3000)).ActiveSteps.ToArray());
        Assert.IsTrue(ScenarioTimeline.At(once, TimeSpan.FromMilliseconds(3500)).Finished);

        // dookola s dlhsim cyklom: druhy cyklus zacina v 10 s
        var loop = once with { Loop = true, CycleMs = 10_000 };
        var moment = ScenarioTimeline.At(loop, TimeSpan.FromMilliseconds(11_500));
        Assert.IsFalse(moment.Finished);
        Assert.AreEqual(1, moment.Cycle);
        CollectionAssert.AreEqual(new[] { 0 }, moment.ActiveSteps.ToArray());
    }

    [TestMethod]
    public void Ucinky_NeskorsiKrokPrepiseAVsetkyTabuleLinky()
    {
        var scenario = Scenario(false, Fault(0, 1000, 1, null, Nak), Fault(0, 1000, 1, 6, NoReply),
            new ScenarioStep(0, 1000, 1, 5, ScenarioAction.PowerLoss), new ScenarioStep(0, 1000, 2, 9, ScenarioAction.LineOutage));

        var effects = ScenarioTimeline.Effects(scenario, [0, 1, 2, 3], line => line == 1 ? ["1-5", "1-6"] : []);

        Assert.AreEqual(Nak, effects.Faults["1-5"]);
        Assert.AreEqual(NoReply, effects.Faults["1-6"]);
        CollectionAssert.AreEqual(new[] { "1-5" }, effects.PoweredOff.ToArray());
        CollectionAssert.AreEqual(new[] { 2 }, effects.Outages.ToArray());
    }

    [TestMethod]
    public void VypadokNapajania_TabulaMlciAPoNavrateJePrazdna()
    {
        var sim = Station();
        sim.Receive(1, ElenFrame.Build(5, 1, Hex("81 10 80 41")));
        Assert.AreNotEqual(0, ((ElenDisplay)sim.Read(s => s.FindBoard("1-5"))!.Display).Rows.Count);

        Assert.IsTrue(sim.SetPower("1-5", false));
        var board = sim.Read(s => s.FindBoard("1-5"))!;
        Assert.IsTrue(board.PoweredOff);
        Assert.IsEmpty(((ElenDisplay)board.Display).Rows);
        // sprava pre vypnutu tabulu: ziadna odpoved ani obsah, samoucenie ju nenahradi
        Assert.IsEmpty(sim.Receive(1, ElenFrame.Build(5, 1, Hex("81 10 80 41"))));
        Assert.IsEmpty(((ElenDisplay)board.Display).Rows);

        sim.SetPower("1-5", true);
        Assert.IsFalse(sim.Read(s => s.FindBoard("1-5"))!.PoweredOff);
        Assert.IsNotEmpty(sim.Receive(1, ElenFrame.Build(5, 1, Hex("81 10 80 41"))));
    }

    [TestMethod]
    public async Task VypadokLinky_SpojenieZavrteANoveSaNeprijima()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var sim = new Simulator();
        sim.SetLine(new LineConfig(2, 4, $"TCP://127.0.0.1:{port}"));
        await using var transport = new TransportManager(sim);
        await transport.SyncAsync();
        using (var first = new TcpClient())
        {
            await first.ConnectAsync(IPAddress.Loopback, port);

            sim.SetLineOutage(2, true);
            await transport.SyncAsync();
            Assert.AreEqual(LineStatus.Outage, sim.Read(s => s.FindLine(2))!.Status);

            // existujuce spojenie je zavrete
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Assert.AreEqual(0, await first.GetStream().ReadAsync(new byte[1], timeout.Token));
        }

        using (var refused = new TcpClient())
            await Assert.ThrowsAsync<SocketException>(() => refused.ConnectAsync(IPAddress.Loopback, port));

        sim.SetLineOutage(2, false);
        await transport.SyncAsync();
        Assert.AreEqual(LineStatus.Listening, sim.Read(s => s.FindLine(2))!.Status);
        using var again = new TcpClient();
        await again.ConnectAsync(IPAddress.Loopback, port);
    }

    [TestMethod]
    public async Task Spustac_KrokyVCaseAPoSkonceniPovodnePoruchy()
    {
        var time = new ManualTime();
        var sim = Station(time);
        var live = new TestLive();
        await using var transport = new TransportManager(sim);
        using var runner = new ScenarioRunner(sim, transport, live, time);
        var scenario = Scenario(false, Fault(1000, 2000, 1, null, Nak), new ScenarioStep(2000, 2000, 1, 5, ScenarioAction.PowerLoss));
        BoardFault FaultOf(string id) => sim.Read(s => s.FindBoard(id))!.Fault;

        await runner.StartAsync(7, scenario);
        Assert.AreEqual(BoardFault.None, FaultOf("1-5"));
        Assert.IsNotNull(runner.Status(7));

        time.Advance(TimeSpan.FromMilliseconds(1000));
        await runner.TickAsync();
        Assert.AreEqual(Nak, FaultOf("1-5"));
        Assert.AreEqual(Nak, FaultOf("1-6"));

        time.Advance(TimeSpan.FromMilliseconds(1500));
        await runner.TickAsync();
        Assert.IsTrue(sim.Read(s => s.FindBoard("1-5"))!.PoweredOff);
        CollectionAssert.AreEqual(new[] { 0, 1 }, runner.Status(7)!.ActiveSteps.ToArray());

        // po kroku poruchy ma tabula 6 zase svoju povodnu poruchu, tabula 5 je stale bez napajania
        time.Advance(TimeSpan.FromMilliseconds(1000));
        await runner.TickAsync();
        Assert.AreEqual(BoardFault.None, FaultOf("1-5"));
        Assert.AreEqual(new BoardFault(FaultKind.Delay, 300), FaultOf("1-6"));
        Assert.IsTrue(sim.Read(s => s.FindBoard("1-5"))!.PoweredOff);

        // koniec scenara: napajanie sa vrati, scenar nebezi
        time.Advance(TimeSpan.FromMilliseconds(1000));
        await runner.TickAsync();
        Assert.IsFalse(sim.Read(s => s.FindBoard("1-5"))!.PoweredOff);
        Assert.IsNull(runner.Status(7));
        Assert.IsGreaterThanOrEqualTo(4, live.Count);
    }

    [TestMethod]
    public async Task Spustac_ZastavenieVratiVsetko()
    {
        var time = new ManualTime();
        var sim = Station(time);
        await using var transport = new TransportManager(sim);
        using var runner = new ScenarioRunner(sim, transport, new TestLive(), time);
        await runner.StartAsync(1, Scenario(true, Fault(0, 5000, 1, 6, Nak), new ScenarioStep(0, 5000, 1, null, ScenarioAction.LineOutage)));
        Assert.AreEqual(Nak, sim.Read(s => s.FindBoard("1-6"))!.Fault);
        Assert.AreEqual(LineStatus.Outage, sim.Read(s => s.FindLine(1))!.Status);

        Assert.IsTrue(await runner.StopAsync(1));

        Assert.AreEqual(new BoardFault(FaultKind.Delay, 300), sim.Read(s => s.FindBoard("1-6"))!.Fault);
        Assert.IsFalse(sim.Read(s => s.FindLine(1))!.Outage);
        Assert.IsFalse(await runner.StopAsync(1));
    }
}
