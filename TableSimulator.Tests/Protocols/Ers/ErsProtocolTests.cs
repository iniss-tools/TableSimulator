using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Ers;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Ers;

/// <summary>
/// Listove tabule ERS (TableClass 2): priklady z docs/iniss/protokoly/ers-fers.mdx, potvrdenie, otacanie listov a stav
/// na dotaz.
/// </summary>
[TestClass]
public class ErsProtocolTests
{
    private const string WriteExample = "16 16 01 05 02 44 4C 30 30 31 31 32 41 30 30 30 30 30 30 03 6A";

    private static (Simulator Sim, ManualTime Time) Create(bool autoLearn = false)
    {
        var time = new ManualTime();
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(2, 2, "TCP://127.0.0.1:47002") { AutoLearn = autoLearn, Manufacturer = "ERS" });
        if (!autoLearn)
            sim.SetBoard(new BoardConfig(2, 5, "ERS") { Profile = BoardProfile.DefaultFor("ERS") with { RotationMs = 3000 } });
        return (sim, time);
    }

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(2, data).SelectMany(r => r.Bytes).ToArray();

    private static FlapDisplay Display(Simulator sim) => (FlapDisplay)sim.Read(s => s.FindBoard("2-5"))!.Display;

    [TestMethod]
    public void Spravy_PrikladyZDokumentacie()
    {
        CollectionAssert.AreEqual(Hex(WriteExample), ErsFrame.Build(5, 1, "12A000000"));
        CollectionAssert.AreEqual(Hex("16 16 01 05 02 44 4C 30 30 31 30 30 30 30 30 30 30 30 30 03 56"), ErsFrame.Build(5, 1, "000000000"));
        CollectionAssert.AreEqual(Hex("16 16 01 05 05"), ErsFrame.Query(5));
    }

    [TestMethod]
    public void Zapis_PotvrdenieOtacanieAStav()
    {
        var (sim, time) = Create();

        CollectionAssert.AreEqual(Hex("16 16 01 05 06"), Replies(sim, Hex(WriteExample)));
        Assert.AreEqual("12A000000", Display(sim).Rows[0]);

        // kym sa listy tocia, tabula este pracuje (CAN); potom stav 01 = dokoncene v poriadku
        CollectionAssert.AreEqual(Hex("16 16 01 05 18"), Replies(sim, ErsFrame.Query(5)));
        time.Advance(TimeSpan.FromSeconds(3));
        CollectionAssert.AreEqual(ErsFrame.Status(5, 1), Replies(sim, ErsFrame.Query(5)));
        CollectionAssert.AreEqual(Hex("16 16 01 05 02 30 30 30 30 31 30 30 30 03 06"), ErsFrame.Status(5, 1));

        // ten isty obsah znova - listy sa neotacaju
        Replies(sim, Hex(WriteExample));
        CollectionAssert.AreEqual(ErsFrame.Status(5, 1), Replies(sim, ErsFrame.Query(5)));
    }

    [TestMethod]
    public void Chyby_NakZlySucetAStavRiadku()
    {
        var (sim, _) = Create();
        var bad = Hex(WriteExample);
        bad[^1] ^= 0x01;

        CollectionAssert.AreEqual(Hex("16 16 01 05 15"), Replies(sim, bad));
        Replies(sim, ErsFrame.Build(5, 0, "000000000"));
        CollectionAssert.AreEqual(ErsFrame.Status(5, ErsProtocol.StatusBadRow), Replies(sim, ErsFrame.Query(5)));

        sim.SetFault("2-5", new BoardFault(FaultKind.Status, 0, 6));
        CollectionAssert.AreEqual(ErsFrame.Status(5, 6), Replies(sim, ErsFrame.Query(5)));
    }

    [TestMethod]
    public void Samoucenie_ListovaTabulaRastiePodlaRiadku()
    {
        var (sim, _) = Create(autoLearn: true);

        Replies(sim, Hex("00 16 16 01 05 02 44 4C 30 30 33 31 32 41 30 30 30 30 30 30 30 30 03 4C"));

        var board = sim.Read(s => s.FindBoard("2-5"))!;
        Assert.AreEqual("ERS", board.ManufacturerKey);
        Assert.AreEqual(4, board.Profile.Rows);
        Assert.AreEqual(11 * 8, board.Profile.Width);
        Assert.AreEqual(1, sim.Read(s => s.Lines.First().Stats.Discarded));
        // prazdny list ERS je 0 - text riadku bez zoznamov ukaze kody
        CollectionAssert.AreEqual(new[] { "", "", "1 | 2 | A", "" }, BoardText.Rows(board).ToArray());
    }
}
