using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Elen;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Elen;

/// <summary>
/// Rozklad obsahu riadku ELEN.
/// </summary>
[TestClass]
public class ElenRowParserTests
{
    [TestMethod]
    public void Riadok_PrikladZDokumentacie()
    {
        var row = ElenRowParser.Parse(Hex("10 80 1B 52 8E 69 6C 69 6E 61 1B 52"), ElenVariant.Elen10);

        Assert.HasCount(1, row.Elements);
        var run = ((RunElement)row.Elements[0]).Run;
        Assert.AreEqual(0, run.X);
        Assert.AreEqual(0x52, run.Font);
        Assert.AreEqual("Žilina", run.Text);
        CollectionAssert.AreEqual(Hex("8E 69 6C 69 6E 61"), run.Codes);
    }

    [TestMethod]
    public void Kurzor_StlpecNad127()
    {
        // stlpec 300 = 12 AC
        var row = ElenRowParser.Parse(Hex("12 AC 41 1B 51 42"), ElenVariant.Elen);

        var runs = row.Elements.Cast<RunElement>().Select(e => e.Run).ToList();
        Assert.AreEqual(300, runs[0].X);
        Assert.AreEqual(0x42, runs[0].Font);
        Assert.IsNull(runs[1].X);
        Assert.AreEqual(0x51, runs[1].Font);
    }

    [TestMethod]
    public void Striedanie_CastiSoSamostatnymKurzorom()
    {
        var row = ElenRowParser.Parse(Hex("1B 52 1B 7B 2A 10 82 41 7C 10 85 42 43 7D 10 A0 5A"), ElenVariant.Elen10);

        Assert.HasCount(2, row.Elements);
        var alternation = (AlternationElement)row.Elements[0];
        Assert.HasCount(2, alternation.Parts);
        Assert.AreEqual(2, alternation.Parts[0][0].X);
        Assert.AreEqual("A", alternation.Parts[0][0].Text);
        Assert.AreEqual(5, alternation.Parts[1][0].X);
        Assert.AreEqual("BC", alternation.Parts[1][0].Text);
        Assert.AreEqual(0x52, alternation.Parts[1][0].Font);
        Assert.AreEqual(32, ((RunElement)row.Elements[1]).Run.X);
        Assert.AreEqual("{A|BC} · Z", row.PlainText);
    }

    [TestMethod]
    public void RozsirenePismo_HornyBajt()
    {
        var row = ElenRowParser.Parse(Hex("10 80 1B 42 19 86 41"), ElenVariant.Elen16);

        Assert.AreEqual(0x8642, ((RunElement)row.Elements[0]).Run.Font);
    }

    [TestMethod]
    public void KodKamenickych_Dekodovanie()
    {
        // "Žilina" v kode Kamenickych: Z = 0x92
        var row = ElenRowParser.Parse(Hex("10 80 92 69 6C 69 6E 61"), ElenVariant.Elen);

        Assert.AreEqual("Žilina", row.PlainText);
    }

    [TestMethod]
    public void ElenOld_BezKodovKurzora()
    {
        // ELENOLD polohuje medzerami; bajt 0x10 je obycajny znak
        var row = ElenRowParser.Parse(Hex("20 20 41 1B 42"), ElenVariant.ElenOld);

        var run = ((RunElement)row.Elements[0]).Run;
        Assert.IsNull(run.X);
        Assert.AreEqual("  A", run.Text);
    }

    [TestMethod]
    public void PrazdnyObsah_PrazdnyRiadok()
    {
        Assert.IsTrue(ElenRowParser.Parse([], ElenVariant.Elen).IsEmpty);
        Assert.IsTrue(ElenRowParser.Parse(Hex("10 80 1B 52"), ElenVariant.Elen).IsEmpty);
    }
}
