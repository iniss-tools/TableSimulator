using System.Text;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Simulation;

/// <summary>
/// Simulator s linkou ELEN - odpovede, samoucenie, hromadne spravy, poruchy.
/// </summary>
[TestClass]
public class SimulatorTests
{
    private static Simulator Create(bool autoLearn = true, LineMode mode = LineMode.Display)
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(3, 4, "TCP://127.0.0.1:47003") { AutoLearn = autoLearn, Mode = mode, Manufacturer = "ELEN10" });
        return sim;
    }

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(3, data).SelectMany(r => r.Bytes).ToArray();

    [TestMethod]
    public void Samoucenie_VytvoriTabuluAPotvrdi()
    {
        var sim = Create();

        var reply = Replies(sim, Hex("01 85 81 80 04 82 84"));

        CollectionAssert.AreEqual(new byte[] { 0x06 }, reply);
        var board = sim.Read(s => s.FindBoard("3-5"));
        Assert.IsNotNull(board);
        Assert.IsTrue(board.AutoCreated);
        Assert.AreEqual("ELEN10", board.ManufacturerKey);
    }

    [TestMethod]
    public void BezSamoucenia_NeznamaAdresaNeodpovie()
    {
        var sim = Create(autoLearn: false);

        Assert.IsEmpty(Replies(sim, Hex("01 85 81 80 04 82 84")));
        Assert.AreEqual(FrameStatus.NoBoard, sim.LogSince(0).Single().Status);
    }

    [TestMethod]
    public void ZapisRiadku_UlozObsahAZvacsiTabulu()
    {
        var sim = Create();

        Replies(sim, ElenFrame.Build(3, 1, Hex("85 10 80 1B 52 8E 69 6C 69 6E 61 1B 52")));

        var board = sim.Read(s => s.FindBoard("3-3"))!;
        var display = (ElenDisplay)board.Display;
        Assert.AreEqual("Žilina", display.Rows[5].PlainText);
        Assert.AreEqual(5, board.Profile.Rows);
    }

    [TestMethod]
    public void ZlySucet_Nak()
    {
        var sim = Create();
        Replies(sim, Hex("01 85 81 80 04 82 84"));

        CollectionAssert.AreEqual(new byte[] { 0x15 }, Replies(sim, Hex("01 85 81 80 04 82 85")));
    }

    [TestMethod]
    public void HromadnaSprava_CasVsetkymBezOdpovede()
    {
        var sim = Create();
        Replies(sim, Hex("01 85 81 80 04 82 84"));
        Replies(sim, ElenFrame.Build(6, 1, [0x80]));

        var reply = Replies(sim, Hex("01 80 80 31 34 33 30 30 35 33 30 30 39 32 36 33 04 80 8E"));

        Assert.IsEmpty(reply);
        var clocks = sim.Read(s => s.Boards.Select(b => b.Display.ClockValue).ToList());
        Assert.HasCount(2, clocks);
        Assert.IsTrue(clocks.All(c => c == new DateTime(2026, 9, 30, 14, 30, 5)));
    }

    [TestMethod]
    public void Jas_APohyblivyRiadok()
    {
        var sim = Create();

        Replies(sim, Hex("01 83 84 80 E3 04 82 85"));

        var display = (ElenDisplay)sim.Read(s => s.FindBoard("3-3"))!.Display;
        Assert.AreEqual(99, display.Brightness);
        Assert.AreEqual(0, display.MovingRow);
    }

    [TestMethod]
    public void PohyblivyText_PrikladZDokumentacie()
    {
        var sim = Create();

        var reply = Replies(sim, Hex("01 87 83 C0 03 54 30 1F 56 6C 61 6B 20 6D 65 9A 6B E1 20 61 73 69 20 32 30 20 6D 69 6E FA 74 20 1F 04 8E 89"));

        CollectionAssert.AreEqual(new byte[] { 0x06 }, reply);
        var display = (ElenDisplay)sim.Read(s => s.FindBoard("3-7"))!.Display;
        Assert.AreEqual("Vlak mešká asi 20 minút", display.MovingText);
    }

    [TestMethod]
    public void PohyblivyText_ViacCasti()
    {
        var sim = Create();
        var text = Encoding.ASCII.GetBytes("T0\u001FAhoj svet \u001F");

        Replies(sim, ElenFrame.Build(7, 3, [0x80, 0x03, .. text[..6]]));
        Replies(sim, ElenFrame.Build(7, 3, [0xC1, .. text[6..]]));

        Assert.AreEqual("Ahoj svet", ((ElenDisplay)sim.Read(s => s.FindBoard("3-7"))!.Display).MovingText);
    }

    [TestMethod]
    public void RezimMonitor_NeodpovedaAleZobrazuje()
    {
        var sim = Create(mode: LineMode.Monitor);

        var reply = Replies(sim, ElenFrame.Build(3, 1, Hex("81 10 80 41")));

        Assert.IsEmpty(reply);
        Assert.AreEqual("A", ((ElenDisplay)sim.Read(s => s.FindBoard("3-3"))!.Display).Rows[1].PlainText);
    }

    [TestMethod]
    [DataRow(FaultKind.NoReply, new byte[0])]
    [DataRow(FaultKind.Nak, new byte[] { 0x15 })]
    [DataRow(FaultKind.Garbage, new byte[] { 0x00 })]
    [DataRow(FaultKind.Delay, new byte[] { 0x06 })]
    public void Porucha_UpraviOdpoved(FaultKind kind, byte[] expected)
    {
        var sim = Create();
        Replies(sim, Hex("01 85 81 80 04 82 84"));
        sim.SetFault("3-5", new BoardFault(kind, 300));

        var replies = sim.Receive(3, Hex("01 85 81 80 04 82 84"));

        CollectionAssert.AreEqual(expected, replies.SelectMany(r => r.Bytes).ToArray());
        if (kind == FaultKind.Delay)
            Assert.AreEqual(TimeSpan.FromMilliseconds(300), replies[0].Delay);
    }

    [TestMethod]
    public void Nastavenie_UlozenieANacitanie()
    {
        var sim = Create();
        sim.SetBoard(new BoardConfig(3, 9, "ELEN16") { Name = "Nástupište 1", Profile = new BoardProfile { Width = 512, Rows = 6, RowHeight = 16 } });

        var copy = new Simulator();
        copy.Load(sim.ToConfig());

        var board = copy.Read(s => s.FindBoard("3-9"))!;
        Assert.AreEqual("Nástupište 1", board.Name);
        Assert.AreEqual(512, board.Profile.Width);
        Assert.AreEqual("ELEN10", copy.Read(s => s.FindLine(3))!.Manufacturer);
    }
}
