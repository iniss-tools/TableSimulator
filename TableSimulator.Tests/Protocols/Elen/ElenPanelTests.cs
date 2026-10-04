using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Elen;

/// <summary>
/// Spravanie panelu ELEN podla programu ELEN Monitor: dvojita adresa, prikazy 2, 5, 7, 8, 9, dva jasy, CR v obsahu, kod 14.
/// </summary>
[TestClass]
public class ElenPanelTests
{
    private static Simulator Create()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001") { AutoLearn = false, Manufacturer = "ELEN10" });
        sim.SetBoard(new BoardConfig(1, 5, "ELEN10") { SecondAddress = 6, Profile = new BoardProfile { Rows = 4 } });
        return sim;
    }

    private static ElenDisplay Display(Simulator sim) => (ElenDisplay)sim.Read(s => s.FindBoard("1-5"))!.Display;

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(1, data).SelectMany(r => r.Bytes).ToArray();

    [TestMethod]
    public void DvojitaAdresa_DruhaOvladaHornuPolovicu()
    {
        var sim = Create();

        CollectionAssert.AreEqual(new byte[] { 0x06 }, Replies(sim, ElenFrame.Build(5, 1, Hex("81 10 80 41"))));
        CollectionAssert.AreEqual(new byte[] { 0x06 }, Replies(sim, ElenFrame.Build(6, 1, Hex("81 10 80 42"))));

        Assert.AreEqual("A", Display(sim).Rows[1].PlainText);
        Assert.AreEqual("B", Display(sim).Rows[3].PlainText);
    }

    [TestMethod]
    public void ObsahSCr_PokracujeNizsimRiadkom()
    {
        var sim = Create();

        Replies(sim, ElenFrame.Build(5, 1, Hex("84 10 80 41 0D 10 80 42")));

        Assert.AreEqual("A", Display(sim).Rows[4].PlainText);
        Assert.AreEqual("B", Display(sim).Rows[3].PlainText);
    }

    [TestMethod]
    public void Prikaz9_CasyBlikania()
    {
        var sim = Create();

        Replies(sim, ElenFrame.Build(5, 9, Hex("8A 85")));

        Assert.AreEqual(10, Display(sim).FlashOn);
        Assert.AreEqual(5, Display(sim).FlashOff);
    }

    [TestMethod]
    public void Prikaz4_DvaJasy()
    {
        var sim = Create();

        Replies(sim, ElenFrame.Build(5, 4, Hex("80 E3 B2")));

        Assert.AreEqual(99, Display(sim).Brightness);
        Assert.AreEqual(50, Display(sim).BrightnessGreen);
    }

    [TestMethod]
    public void Prikaz5_OdpovedSDatami()
    {
        var sim = Create();

        var reply = Replies(sim, ElenFrame.Build(5, 5, []));

        // 02 adresa prikaz stav stav 04 sucet - sucet ako pri sprave od INISSu
        CollectionAssert.AreEqual(Hex("02 85 85 80 80 04"), reply[..6]);
        var (high, low) = ElenChecksum.Encode(ElenChecksum.Compute(reply.AsSpan(0, 6)));
        CollectionAssert.AreEqual(new[] { high, low }, reply[6..]);
    }

    [TestMethod]
    public void Prikaz2_CitanieRiadku()
    {
        var sim = Create();
        Replies(sim, ElenFrame.Build(5, 1, Hex("82 10 80 41")));

        var reply = Replies(sim, ElenFrame.Build(5, 2, Hex("82")));

        CollectionAssert.AreEqual(Hex("02 85 82 82 10 80 41 04"), reply[..8]);
    }

    [TestMethod]
    public void Prikaz7_OdpovedSiedemF()
    {
        CollectionAssert.AreEqual(new byte[] { 0x7F }, Replies(Create(), ElenFrame.Build(5, 7, [])));
    }

    [TestMethod]
    public void Kod14_ElenAtributElekonKurzor()
    {
        var elen = ElenRowParser.Parse(Hex("10 80 1B 52 41 14 09 42"), ElenVariant.Elen10);
        var elekon = ElenRowParser.Parse(Hex("14 85 41"), ElenVariant.Elekon);

        var runs = elen.Elements.Cast<RunElement>().Select(e => e.Run).ToList();
        Assert.AreEqual(0x52, runs[0].Font);
        // 14 09: farba 1 (cervena), bit 3 = rez z bitov 4-5 (0)
        Assert.AreEqual(0x41, runs[1].Font);
        Assert.AreEqual(517, ((RunElement)elekon.Elements[0]).Run.X);
    }

    [TestMethod]
    public void Striedanie_LubovolnyBajtPocitadla()
    {
        var row = ElenRowParser.Parse(Hex("1B 7B 2B 41 7C 42 7D"), ElenVariant.Elen10);

        Assert.AreEqual("{A|B}", row.PlainText);
    }
}
