using TableSimulator.Core.Protocols.Elen;

namespace TableSimulator.Tests.Protocols.Elen;

/// <summary>
/// Ramec a kontrolny sucet ELEN - priklady z docs/iniss/protokoly/elen.mdx.
/// </summary>
[TestClass]
public class ElenFrameTests
{
    internal static byte[] Hex(string hex) => Convert.FromHexString(hex.Replace(" ", "", StringComparison.Ordinal));

    [TestMethod]
    [DataRow("01 85 81 80 04", "82 84")] // zmazanie tabule 5
    [DataRow("01 83 81 82 10 80 1B 52 8E 69 6C 69 6E 61 1B 52 04", "8A 88")] // ELEN10 "Zilina"
    [DataRow("01 83 84 80 E3 04", "82 85")] // jas 99
    [DataRow("01 80 80 31 34 33 30 30 35 33 30 30 39 32 36 33 04", "80 8E")] // cas
    [DataRow("01 87 83 C0 03 54 30 1F 56 6C 61 6B 20 6D 65 9A 6B E1 20 61 73 69 20 32 30 20 6D 69 6E FA 74 20 1F 04", "8E 89")] // pohyblivy text
    public void Sucet_PodlaDokumentacie(string frame, string sum)
    {
        var (high, low) = ElenChecksum.Encode(ElenChecksum.Compute(Hex(frame)));

        CollectionAssert.AreEqual(Hex(sum), new[] { high, low });
    }

    [TestMethod]
    public void Zostavenie_ZmazanieTabule()
    {
        CollectionAssert.AreEqual(Hex("01 85 81 80 04 82 84"), ElenFrame.Build(5, 1, [0x80]));
    }

    [TestMethod]
    public void Skladanie_PoCastiachAViacNaraz()
    {
        var framer = new ElenFramer();
        var a = Hex("01 85 81 80 04 82 84");
        var b = Hex("01 83 84 80 E3 04 82 85");
        var stream = a.Concat(b).ToArray();

        Assert.IsEmpty(framer.Push(stream.AsSpan(0, 4)));
        var frames = framer.Push(stream.AsSpan(4));

        Assert.HasCount(2, frames);
        Assert.AreEqual(5, frames[0].Address);
        Assert.AreEqual(1, frames[0].Command);
        CollectionAssert.AreEqual(new byte[] { 0x80 }, frames[0].Parameters);
        Assert.IsTrue(frames[1].ChecksumOk);
        Assert.AreEqual(4, frames[1].Command);
    }

    [TestMethod]
    public void Skladanie_SumAZlySucet()
    {
        var framer = new ElenFramer();

        var frames = framer.Push(Hex("FF 06 01 85 81 80 04 82 85"));

        Assert.HasCount(1, frames);
        Assert.IsFalse(frames[0].ChecksumOk);
        Assert.AreEqual(2, framer.DiscardedBytes);
    }

    [TestMethod]
    public void Skladanie_NovyZaciatokUprostredSpravy()
    {
        var framer = new ElenFramer();

        var frames = framer.Push(Hex("01 85 81 01 85 81 80 04 82 84"));

        Assert.HasCount(1, frames);
        Assert.IsTrue(frames[0].ChecksumOk);
        Assert.AreEqual(3, framer.DiscardedBytes);
    }
}
