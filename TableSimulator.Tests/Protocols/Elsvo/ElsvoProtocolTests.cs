using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Elsvo;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Elsvo;

/// <summary>
/// Spinacie jednotky zosilnovacov ELSVO (TableClass 129): priklady z docs/iniss/protokoly/elsvo.mdx - ramec, zopnutie
/// v pasme, dotaz, uvolnenie, zly sucet a poruchy.
/// </summary>
[TestClass]
public class ElsvoProtocolTests
{
    private static Simulator Create(bool autoLearn = false)
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(3, 129, "TCP://127.0.0.1:47003") { AutoLearn = autoLearn, Manufacturer = "ELSVO" });
        if (!autoLearn)
            sim.SetBoard(new BoardConfig(3, 5, "ELSVO"));
        return sim;
    }

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(3, data).SelectMany(r => r.Bytes).ToArray();

    private static AmplifierDisplay Amplifier(Simulator sim) => (AmplifierDisplay)sim.Read(s => s.FindBoard("3-5"))!.Display;

    [TestMethod]
    public void Spravy_PrikladyZDokumentacie()
    {
        CollectionAssert.AreEqual(Hex("02 05 1F 03 D7"), ElsvoFrame.Build(5, ElsvoFrame.On));
        CollectionAssert.AreEqual(Hex("02 05 3F 03 B7"), ElsvoFrame.Build(5, ElsvoFrame.OnBand2));
        CollectionAssert.AreEqual(Hex("02 05 80 03 76"), ElsvoFrame.Build(5, ElsvoFrame.Query));
        CollectionAssert.AreEqual(Hex("02 05 C0 03 36"), ElsvoFrame.Build(5, ElsvoFrame.Release));
        CollectionAssert.AreEqual(Hex("02 05 00 03 F6"), ElsvoFrame.Build(5, ElsvoFrame.Off));
        CollectionAssert.AreEqual(Hex("02 05 90 03 66"), ElsvoFrame.Build(5, 0x90));
        Assert.AreEqual("Ready Hlaseni", ElsvoProtocol.StateBits(0x90));
    }

    [TestMethod]
    public void Zopnutie_DotazUvolnenie()
    {
        var sim = Create();
        var start = DateTimeOffset.Now;

        CollectionAssert.AreEqual(Hex("02 05 90 03 66"), Replies(sim, Hex("02 05 1F 03 D7")));
        CollectionAssert.AreEqual(Hex("02 05 90 03 66"), Replies(sim, Hex("02 05 80 03 76")));
        Assert.AreEqual(1, Amplifier(sim).Band);
        Assert.IsTrue(Amplifier(sim).OnSince >= start);
        CollectionAssert.AreEqual(new[] { "ON 1" }, BoardText.Rows(sim.Read(s => s.FindBoard("3-5"))!).ToArray());

        // druhe pasmo pocas zopnutia nie je nove zopnutie
        Replies(sim, Hex("02 05 3F 03 B7"));
        Assert.AreEqual(2, Amplifier(sim).Band);
        Assert.AreEqual(1, Amplifier(sim).Switches);

        CollectionAssert.AreEqual(Hex("02 05 80 03 76"), Replies(sim, Hex("02 05 C0 03 36")));
        Assert.AreEqual(0, Amplifier(sim).Band);
        Assert.AreEqual(ElsvoFrame.Release, Amplifier(sim).LastCommand);
        Assert.IsNull(Amplifier(sim).OnSince);

        Replies(sim, Hex("02 05 1F 03 D7"));
        CollectionAssert.AreEqual(Hex("02 05 80 03 76"), Replies(sim, Hex("02 05 00 03 F6")));
        Assert.AreEqual(2, Amplifier(sim).Switches);
    }

    [TestMethod]
    public void Chyby_SucetPoruchaSum()
    {
        var sim = Create();

        // zly sucet: prikaz sa nevykona, odpoved s bitom chyby prenosu
        CollectionAssert.AreEqual(ElsvoFrame.Build(5, 0xA0), Replies(sim, Hex("02 05 1F 03 D8")));
        Assert.AreEqual(0, Amplifier(sim).Band);

        // porucha so zvolenym stavom: pripravena, ale bez zopnutia
        sim.SetFault("3-5", new BoardFault(FaultKind.Status, 0, 0x84));
        CollectionAssert.AreEqual(ElsvoFrame.Build(5, 0x84), Replies(sim, Hex("02 05 1F 03 D7")));
        Assert.AreEqual(1, Amplifier(sim).Band, "jednotka prikaz vykona, len hlasi iny stav");

        sim.SetFault("3-5", new BoardFault(FaultKind.Nak, 0, 0));
        CollectionAssert.AreEqual(ElsvoFrame.Build(5, 0xB0), Replies(sim, Hex("02 05 80 03 76")));

        // sum pred spravou a sprava po kusoch
        sim.SetFault("3-5", BoardFault.None);
        Assert.IsEmpty(Replies(sim, Hex("FF 00 02 05 C0")));
        CollectionAssert.AreEqual(Hex("02 05 80 03 76"), Replies(sim, Hex("03 36")));
        Assert.AreEqual(2, sim.Read(s => s.Lines.First().Stats.Discarded));
    }

    [TestMethod]
    public void Samoucenie_AdresaNulaAJednotkaNaKazdejAdrese()
    {
        var sim = Create(autoLearn: true);

        CollectionAssert.AreEqual(ElsvoFrame.Build(0, 0x90), Replies(sim, ElsvoFrame.Build(0, ElsvoFrame.On)));
        Replies(sim, ElsvoFrame.Build(7, ElsvoFrame.Query));

        var boards = sim.Read(s => s.Boards.Select(b => (b.Id, b.ManufacturerKey, b.Display.GetType())).ToList());
        CollectionAssert.AreEquivalent(new[] { ("3-0", "ELSVO", typeof(AmplifierDisplay)), ("3-7", "ELSVO", typeof(AmplifierDisplay)) }, boards);
    }
}
