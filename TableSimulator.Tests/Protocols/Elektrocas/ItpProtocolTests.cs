using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Elektrocas;

/// <summary>
/// Linka cez prevodnik KTP (TableClass 130) - priklad z docs/iniss/protokoly/itp.mdx.
/// </summary>
[TestClass]
public class ItpProtocolTests
{
    private const string ClearPacket = "F1 8F 09 00 A0 00 01 00 0C 05 00 40 51 00 07 46 E1 8E";

    private static Simulator Create()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, 130, "TCP://127.0.0.1:47001") { AutoLearn = false, Manufacturer = "LCD1" });
        sim.SetBoard(new BoardConfig(1, 12, "LCD1") { Profile = BoardProfile.DefaultFor("LCD1") });
        return sim;
    }

    private static ItpPacket ReplyPacket(Simulator sim, byte[] data)
    {
        var packets = new ItpFramer().Push(sim.Receive(1, data).SelectMany(r => r.Bytes).ToArray());
        Assert.AreEqual(1, packets.Count);
        Assert.IsTrue(packets[0].ChecksumOk);
        return packets[0];
    }

    [TestMethod]
    public void Paket_ZostavenieZDokumentacie()
    {
        CollectionAssert.AreEqual(Hex(ClearPacket), ItpPacket.Build(0xA0, 1, Hex("0C 05 00 40 51")));
    }

    [TestMethod]
    public void Paket_SkladaSaPoCastiach()
    {
        var framer = new ItpFramer();
        var raw = Hex("00 " + ClearPacket);

        Assert.AreEqual(0, framer.Push(raw.AsSpan(0, 7)).Count);
        var packets = framer.Push(raw.AsSpan(7));

        Assert.AreEqual(1, packets.Count);
        Assert.IsTrue(packets[0].ChecksumOk);
        Assert.AreEqual(1, packets[0].Sequence);
        CollectionAssert.AreEqual(Hex("0C 05 00 40 51 00"), packets[0].Payload);
        Assert.AreEqual(1, framer.DiscardedBytes);
    }

    [TestMethod]
    public void Odpoved_ZabalenaSTymIstymCislom()
    {
        var sim = Create();

        var reply = ReplyPacket(sim, Hex(ClearPacket));

        Assert.IsFalse(reply.IsCommand);
        Assert.AreEqual(ItpPacket.KtpProtocol, reply.Protocol);
        Assert.AreEqual(1, reply.Kind >> 8);
        Assert.AreEqual(1, reply.Sequence);
        // hlavicka s kodom chyby 0, za nou odpoved tabule doplnena na parny pocet bajtov
        CollectionAssert.AreEqual(Hex("00 00 00 06 0C 70 00 82"), reply.Payload);
    }

    [TestMethod]
    public void HromadnaSprava_PrevodnikHlasiZeZariadenieNeodpovedalo()
    {
        var sim = Create();

        var reply = ReplyPacket(sim, ItpPacket.Build(0xA0, 7, ElektrocasFrame.Build(0xFF, 0, 0x40, [])));

        Assert.AreEqual(7, reply.Sequence);
        CollectionAssert.AreEqual(new byte[] { ItpProtocol.KtpNoAnswer, 0 }, reply.Payload);
    }

    [TestMethod]
    public void TabulaNeodpoveda_PrevodnikHlasiChybu()
    {
        var sim = Create();
        sim.SetFault("1-12", new BoardFault(FaultKind.NoReply));

        var reply = ReplyPacket(sim, Hex(ClearPacket));

        CollectionAssert.AreEqual(new byte[] { ItpProtocol.KtpNoAnswer, 0 }, reply.Payload);
    }

    [TestMethod]
    public void ZlySucetPaketu_BezOdpovede()
    {
        var sim = Create();
        var raw = Hex(ClearPacket);
        raw[^3] ^= 1;

        Assert.AreEqual(0, sim.Receive(1, raw).Count);
    }
}
