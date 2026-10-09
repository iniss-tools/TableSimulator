using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Apel;
using TableSimulator.Core.Protocols.Remote;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Apel;

/// <summary>
/// Tabule APEL a APEL_AN (TableClass 10): priklady z docs/iniss/protokoly/apel.mdx - ramec so zdvojovanim, odpoved,
/// zapis textu, zmazanie viacerymi zaznamami, cas, jas, hromadne spravy a poruchy.
/// </summary>
[TestClass]
public class ApelProtocolTests
{
    private static Simulator Create(string manufacturer = "APEL", bool autoLearn = false)
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(10, 10, "TCP://127.0.0.1:47010") { AutoLearn = autoLearn, Manufacturer = manufacturer });
        if (!autoLearn)
            sim.SetBoard(new BoardConfig(10, 0x81, manufacturer) { Profile = BoardProfile.DefaultFor(manufacturer) });
        return sim;
    }

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(10, data).SelectMany(r => r.Bytes).ToArray();

    private static SimBoard Board(Simulator sim) => sim.Read(s => s.FindBoard("10-129"))!;

    [TestMethod]
    public void Spravy_PrikladyZDokumentacie()
    {
        CollectionAssert.AreEqual(Hex("10 02 81 00 02 4F 4B 06 10 03"), ApelFrame.Build(0x81, new ApelRecord(0, "OK"u8.ToArray())));
        CollectionAssert.AreEqual(Hex("10 02 81 01 06 42 72 65 7A 6E 6F 29 10 03"), ApelFrame.Build(0x81, new ApelRecord(1, "Brezno"u8.ToArray())));
        CollectionAssert.AreEqual(Hex("10 02 81 83 01 05 87 10 03"), ApelFrame.Build(0x81, new ApelRecord(0x83, [5])));
        CollectionAssert.AreEqual(Hex("10 02 81 8A 00 8A 10 03"), ApelFrame.Build(0x81, new ApelRecord(0x8A, [])));
        CollectionAssert.AreEqual(Hex("10 02 80 81 06 87 10 03"), ApelFrame.Reply(0x80, 0x81, ApelFrame.StateOk));
        // bajt mesiaca 10 je zdvojeny
        CollectionAssert.AreEqual(Hex("10 02 81 81 07 05 30 14 01 10 10 20 26 B0 10 03"),
            ApelFrame.Build(0x81, new ApelRecord(0x81, Hex("05 30 14 01 10 20 26"))));
    }

    [TestMethod]
    public void Text_JednoriadkovaZasobnik00AOdpovedOk()
    {
        var sim = Create();

        CollectionAssert.AreEqual(Hex("10 02 80 81 06 87 10 03"), Replies(sim, Hex("10 02 81 00 02 4F 4B 06 10 03")));
        // iny zasobnik jednoriadkova tabula len potvrdi, ukazuje 00
        Replies(sim, ApelFrame.Build(0x81, new ApelRecord(3, "XY"u8.ToArray())));

        CollectionAssert.AreEqual(new[] { "OK" }, BoardText.Rows(Board(sim)).ToArray());
    }

    [TestMethod]
    public void Text_ViacriadkovaPodlaCislaRiadkuAZmazanie()
    {
        var sim = Create("APEL_AN");
        Replies(sim, Hex("10 02 81 01 06 42 72 65 7A 6E 6F 29 10 03"));
        Replies(sim, ApelFrame.Build(0x81, new ApelRecord(0, ToolsCore.Iniss.Tools.Encodings.Win1250.GetBytes("Žilina"))));

        CollectionAssert.AreEqual(new[] { "Žilina", "Brezno", "", "", "", "" }, BoardText.Rows(Board(sim)).ToArray());

        // prepisanie riadku celym textom, potom zmazanie prazdnymi zapismi do viacerych riadkov v jednej sprave
        Replies(sim, ApelFrame.Build(0x81, new ApelRecord(1, "Ko"u8.ToArray())));
        Assert.AreEqual("Ko", BoardText.Rows(Board(sim))[1]);
        var reply = Replies(sim, ApelFrame.Build(0x81, Enumerable.Range(0, 6).Select(r => new ApelRecord(r, [])).ToArray()));
        CollectionAssert.AreEqual(ApelFrame.Reply(0x80, 0x81, ApelFrame.StateOk), reply);
        Assert.IsTrue(BoardText.Rows(Board(sim)).All(r => r.Length == 0));
    }

    [TestMethod]
    public void CasAHromadnaSprava_BezOdpovede()
    {
        var sim = Create();
        sim.SetBoard(new BoardConfig(10, 0x82, "APEL") { Profile = BoardProfile.DefaultFor("APEL") });

        Replies(sim, Hex("10 02 81 81 07 05 30 14 01 10 10 20 26 B0 10 03"));
        Assert.AreEqual(new DateTime(2026, 10, 1, 14, 30, 5), Board(sim).Display.ClockValue);

        // hromadne zmazanie a zapis: vsetky tabule, ziadna odpoved
        Assert.IsEmpty(Replies(sim, ApelFrame.Build(0xFF, new ApelRecord(0, "VSETKY"u8.ToArray()))));
        CollectionAssert.AreEqual(new[] { "VSETKY" }, BoardText.Rows(sim.Read(s => s.FindBoard("10-130"))!).ToArray());
    }

    [TestMethod]
    public void Chyby_SucetPoruchaSamoucenie()
    {
        var sim = Create();
        var bad = Hex("10 02 81 00 02 4F 4B 07 10 03");
        CollectionAssert.AreEqual(ApelFrame.Reply(0x80, 0x81, ApelFrame.StateTransmissionError), Replies(sim, bad));

        sim.SetFault("10-129", new BoardFault(FaultKind.Status, 0, ApelFrame.StateBusy));
        CollectionAssert.AreEqual(ApelFrame.Reply(0x80, 0x81, ApelFrame.StateBusy), Replies(sim, Hex("10 02 81 8A 00 8A 10 03")));

        var learning = Create(autoLearn: true);
        Replies(learning, Hex("00 10 02 81 00 02 4F 4B 06 10 03"));
        var board = learning.Read(s => s.FindBoard("10-129"))!;
        Assert.AreEqual("APEL", board.ManufacturerKey);
        Assert.IsInstanceOfType<CharDisplay>(board.Display);
        Assert.AreEqual(1, learning.Read(s => s.Lines.First().Stats.Discarded));
    }

    [TestMethod]
    public void VzdialenyIniss_ObsahBezRamca()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(4, 128, "TCP://127.0.0.1:8090") { AutoLearn = true });
        // riadiaci INISS posiela obsah bez ramca DLE a bez suctu
        var reply = RemoteMessage.Parse(sim.Receive(4, RemoteMessage.Build(RemoteMessage.Request, 1,
            [RemoteChunk.FromInt32("TCLS", 11), RemoteChunk.FromInt32("TADR", 0x81), new RemoteChunk("TCMD", Hex("81 02 05 50 72 61 68 61"))]))[0].Bytes);

        Assert.AreEqual(0, reply.Find("TSTA")!.Int32 & 0xFF);
        var board = sim.Read(s => s.FindBoard("4-129"))!;
        Assert.AreEqual("APEL_AN", board.ManufacturerKey);
        Assert.AreEqual("Praha", BoardText.Rows(board)[2]);
    }
}
