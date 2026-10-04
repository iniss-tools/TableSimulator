using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Elektrocas;

/// <summary>
/// Tabule Elektrocas LCD1 a LCD (TableClass 5 a 1) - priklady z docs/iniss/protokoly/lcd1.mdx.
/// </summary>
[TestClass]
public class ElektrocasProtocolTests
{
    private static Simulator Create(int tableClass = 5, string manufacturer = "LCD1")
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, tableClass, "TCP://127.0.0.1:47001") { AutoLearn = false, Manufacturer = manufacturer });
        sim.SetBoard(new BoardConfig(1, 12, manufacturer) { Profile = BoardProfile.DefaultFor(manufacturer) });
        sim.SetBoard(new BoardConfig(1, 13, manufacturer) { Profile = BoardProfile.DefaultFor(manufacturer) });
        return sim;
    }

    private static CharDisplay Display(Simulator sim, int address = 12) =>
        (CharDisplay)sim.Read(s => s.FindBoard($"1-{address}"))!.Display;

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(1, data).SelectMany(r => r.Bytes).ToArray();

    private static string Text(CharRow row, int layer = 0) =>
        new(row.Layers[layer].Select(c => c.IsSet ? (char)c.Code : '\0').ToArray());

    [TestMethod]
    public void Ramec_SucetAOdpovedZDokumentacie()
    {
        CollectionAssert.AreEqual(Hex("0C 05 00 40 51"), ElektrocasFrame.Build(0x0C, 0, 0x40, []));
        CollectionAssert.AreEqual(Hex("00 06 0C 70 00 82"), ElektrocasFrame.Build(0, 0x0C, 0x70, [0]));
    }

    [TestMethod]
    public void Ramec_SkladaSaPoCastiach()
    {
        var framer = new ElektrocasFramer();

        Assert.AreEqual(0, framer.Push(Hex("0C 05")).Count);
        var frames = framer.Push(Hex("00 40 51 0D 05 00 40 52"));

        Assert.AreEqual(2, frames.Count);
        Assert.AreEqual(0x40, frames[0].Command);
        Assert.IsTrue(frames[0].ChecksumOk);
        Assert.AreEqual(0x0D, frames[1].Address);
    }

    [TestMethod]
    public void SumPredSpravou_ZahodiSaPoMedzere()
    {
        var time = new ManualTime();
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(1, 5, "TCP://127.0.0.1:47001") { AutoLearn = false, Manufacturer = "LCD1" });
        sim.SetBoard(new BoardConfig(1, 12, "LCD1") { Profile = BoardProfile.DefaultFor("LCD1") });

        Assert.AreEqual(0, Replies(sim, Hex("02")).Length);
        Assert.AreEqual(0, Replies(sim, Hex("0C 05 00 40 51")).Length);
        time.Advance(TimeSpan.FromSeconds(1));

        CollectionAssert.AreEqual(Hex("00 06 0C 70 00 82"), Replies(sim, Hex("0C 05 00 40 51")));
        Assert.AreEqual(6, sim.Read(s => s.Lines.First().Stats.Discarded));
    }

    [TestMethod]
    public void Zmazanie_TabulaPotvrdiStavom()
    {
        var sim = Create();
        Replies(sim, Hex("0C 19 00 16 02 00 01 00 4F 73 20 33 34 31 32 20 12 12 12 12 12 12 12 12 9A"));

        CollectionAssert.AreEqual(Hex("00 06 0C 70 00 82"), Replies(sim, Hex("0C 05 00 40 51")));
        Assert.AreEqual(0, Display(sim).Rows.Count);
    }

    [TestMethod]
    public void Zapis16_TextSPismomKazdehoZnaku()
    {
        var sim = Create();

        var reply = Replies(sim, Hex("0C 19 00 16 02 00 01 00 4F 73 20 33 34 31 32 20 12 12 12 12 12 12 12 12 9A"));

        CollectionAssert.AreEqual(Hex("00 06 0C 70 00 82"), reply);
        var row = Display(sim).Rows[2];
        Assert.AreEqual("Os 3412 ", Text(row));
        Assert.IsTrue(row.Layers[0].All(c => c.Font == 0x12));
    }

    [TestMethod]
    public void Zapis16_VrstvyAPrepisZakladuIchZrusi()
    {
        var sim = Create();
        Replies(sim, Frame(12, 0x16, [0, 0, 1, 0, .. "ABCD"u8, 1, 1, 1, 1]));

        // dve vrstvy v jednej sprave: vrstva 1 a 2 od stlpca 1, po 2 znaky
        Replies(sim, Frame(12, 0x16, [0, 1, 2, 1, .. "XY"u8, 2, 2, 2, .. "QR"u8, 3, 3]));

        var row = Display(sim).Rows[0];
        Assert.AreEqual("\0XY", Text(row, 1));
        Assert.AreEqual("\0QR", Text(row, 2));
        Assert.AreEqual(3, row.Layers[2][1].Font);

        Replies(sim, Frame(12, 0x16, [0, 0, 1, 0, .. "EFGH"u8, 1, 1, 1, 1]));

        Assert.AreEqual(1, row.Layers.Count);
        Assert.AreEqual("EFGH", Text(row));
    }

    [TestMethod]
    public void Zapis15_Useky()
    {
        var sim = Create();

        var reply = Replies(sim, Frame(12, 0x15, [1, 0, 0, 0x10, 2, .. "AB"u8, 5, 0, 0x11, 1, .. "C"u8]));

        CollectionAssert.AreEqual(Status(12, 0), reply);
        var cells = Display(sim).Rows[1].Layers[0];
        Assert.AreEqual("AB\0\0\0C", Text(Display(sim).Rows[1]));
        Assert.AreEqual(0x10, cells[1].Font);
        Assert.AreEqual(0x11, cells[5].Font);
    }

    [TestMethod]
    public void Zapis10_LcdBezPisma()
    {
        var sim = Create(1, "LCD");

        var reply = Replies(sim, Frame(12, 0x10, [3, 2, .. "Os"u8]));

        CollectionAssert.AreEqual(Status(12, 0), reply);
        Assert.AreEqual("\0\0Os", Text(Display(sim).Rows[3]));
    }

    [TestMethod]
    public void RiadokMimoTabule_ChybneSuradnice()
    {
        var sim = Create();

        var reply = Replies(sim, Frame(12, 0x16, [4, 0, 1, 0, .. "A"u8, 1]));

        CollectionAssert.AreEqual(Status(12, ElektrocasProtocol.StatusBadCoordinates), reply);
    }

    [TestMethod]
    public void ZlySucet_ChybnaSprava()
    {
        var sim = Create();

        CollectionAssert.AreEqual(Status(12, ElektrocasProtocol.StatusBadMessage), Replies(sim, Hex("0C 05 00 40 52")));
    }

    [TestMethod]
    public void PosunRiadkov_VlozenieAZmazanie()
    {
        var sim = Create();
        Replies(sim, Frame(12, 0x16, [0, 0, 1, 0, .. "A"u8, 1]));
        Replies(sim, Frame(12, 0x16, [1, 0, 1, 0, .. "B"u8, 1]));
        Replies(sim, Frame(12, 0x16, [3, 0, 1, 0, .. "D"u8, 1]));

        Replies(sim, Frame(12, 0x30, [1]));

        Assert.AreEqual("A", Text(Display(sim).Rows[0]));
        Assert.IsFalse(Display(sim).Rows.ContainsKey(1));
        Assert.AreEqual("B", Text(Display(sim).Rows[2]));
        // posledny riadok vypadol
        Assert.AreEqual(2, Display(sim).Rows.Count);

        Replies(sim, Frame(12, 0x31, [0]));

        Assert.AreEqual("B", Text(Display(sim).Rows[1]));
        Assert.AreEqual(1, Display(sim).Rows.Count);
    }

    [TestMethod]
    public void Osvetlenie()
    {
        var sim = Create();

        Replies(sim, Frame(12, 0x50, [0]));
        Assert.AreEqual(0, Display(sim).Brightness);

        CollectionAssert.AreEqual(Status(12, 0), Replies(sim, Hex("0C 06 00 50 01 63")));
        Assert.AreEqual(100, Display(sim).Brightness);
    }

    [TestMethod]
    public void TypZariadenia_Odpoved78()
    {
        var sim = Create();

        var reply = Replies(sim, Frame(12, 0x08, []));

        CollectionAssert.AreEqual(ElektrocasFrame.Build(0, 12, 0x78, [3, 9, 1, 0]), reply);
    }

    [TestMethod]
    public void HromadnaSprava_BezOdpovede()
    {
        var sim = Create();
        Replies(sim, Frame(12, 0x16, [0, 0, 1, 0, .. "A"u8, 1]));
        Replies(sim, Frame(13, 0x16, [0, 0, 1, 0, .. "B"u8, 1]));

        Assert.AreEqual(0, Replies(sim, Frame(0xFF, 0x40, [])).Length);

        Assert.AreEqual(0, Display(sim, 12).Rows.Count);
        Assert.AreEqual(0, Display(sim, 13).Rows.Count);
    }

    [TestMethod]
    public void Cas_NastaviHodinyBezOdpovede()
    {
        var sim = Create();

        var reply = Replies(sim, Hex("FA 0D 00 A8 00 05 30 14 30 69 26 51 08"));

        Assert.AreEqual(0, reply.Length);
        Assert.AreEqual(new DateTime(2026, 9, 30, 14, 30, 5), Display(sim, 12).ClockValue);
        Assert.AreEqual(new DateTime(2026, 9, 30, 14, 30, 5), Display(sim, 13).ClockValue);
    }

    [TestMethod]
    public void DotazNaCas_OdpovedA8NaRtcBroadcastAddr()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, 5, "TCP://127.0.0.1:47001") { AutoLearn = true, Manufacturer = "LCD1" });

        var reply = Replies(sim, Hex("21 05 00 A1 C7"));

        CollectionAssert.AreEqual(Hex("FA 0D 21 A8"), reply[..4]);
        Assert.AreEqual(ElektrocasFrame.Checksum(reply.AsSpan(0, reply.Length - 1)), reply[^1]);
        Assert.IsNotNull(ElektrocasProtocol.ParseTime(reply[4..^1]));
    }

    [TestMethod]
    public void CasBcd_TamASpat()
    {
        var time = new DateTime(2026, 9, 30, 14, 30, 5, 120);

        var bytes = ElektrocasProtocol.TimeBytes(time);

        CollectionAssert.AreEqual(Hex("12 05 30 14 30 69 26"), bytes[..7]);
        Assert.AreEqual(time, ElektrocasProtocol.ParseTime(bytes));
    }

    [TestMethod]
    public void Porucha_StavoveBity()
    {
        var sim = Create();
        sim.SetFault("1-12", new BoardFault(FaultKind.Status, Status: 0x48));

        CollectionAssert.AreEqual(Status(12, 0x48), Replies(sim, Hex("0C 05 00 40 51")));
    }

    [TestMethod]
    public void Lcd1_SipkyVZazname()
    {
        Assert.AreEqual("«A»", ElektrocasCharset.Decode(Hex("84 41 85"), "LCD1"));
        Assert.AreNotEqual("«A»", ElektrocasCharset.Decode(Hex("84 41 85"), "LCD"));
    }

    private static byte[] Frame(int address, int command, byte[] parameters) => ElektrocasFrame.Build(address, 0, command, parameters);

    private static byte[] Status(int address, int status) => ElektrocasFrame.Build(0, address, 0x70, [(byte)status]);
}
