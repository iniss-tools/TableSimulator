using System.Text;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Protocols.Remote;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Remote;

/// <summary>
/// Linka vzdialeneho INISSu (TableClass 128): obal sprav, sucet, prikazy pre tabule roznych rodin na jednej linke,
/// stav tabule v odpovedi, fronty hlaseni - podla docs/iniss/protokoly/vzdialeny-iniss.mdx.
/// </summary>
[TestClass]
public class RemoteProtocolTests
{
    private static Simulator Create(bool autoLearn = true)
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(4, 128, "TCP://127.0.0.1:8090") { AutoLearn = autoLearn });
        return sim;
    }

    /// <summary>Prikaz pre tabulu tak, ako ho posle riadiaci INISS - kontrolny sucet spravy tabule nevyplneny.</summary>
    private static RemoteChunk[] TableCommand(int manufacturer, int address, byte[] frame, int checksumBytes)
    {
        var command = (byte[])frame.Clone();
        command.AsSpan(command.Length - checksumBytes).Fill(0xCC);
        return [RemoteChunk.FromInt32("TCLS", manufacturer), RemoteChunk.FromInt32("TADR", address), new RemoteChunk("TCMD", command)];
    }

    private static RemoteMessage Send(Simulator sim, uint seq, params RemoteChunk[] chunks)
    {
        var replies = sim.Receive(4, RemoteMessage.Build(RemoteMessage.Request, seq, chunks));
        Assert.HasCount(1, replies);
        var reply = RemoteMessage.Parse(replies[0].Bytes);
        Assert.IsTrue(reply.CrcOk);
        Assert.AreEqual(RemoteMessage.Reply, reply.Type);
        Assert.AreEqual(seq, reply.Sequence);
        return reply;
    }

    private static int Status(RemoteMessage reply) => reply.Find("TSTA")!.Int32!.Value;

    [TestMethod]
    public void Sucet_PrikladyZoSpecifikacie()
    {
        Assert.AreEqual(0xBEEF, RemoteCrc.Compute("123456789"u8));
        Assert.AreEqual(0x0053, RemoteCrc.Compute([0x53]));
        CollectionAssert.AreEqual(Hex("53 0B000000 00000000 B7 F1"), RemoteMessage.Build(RemoteMessage.Request, 0, []));
    }

    [TestMethod]
    public void Sprava_ZapisTabuleAOdpovedZoSpecifikacie()
    {
        var request = RemoteMessage.Build(RemoteMessage.Request, 7,
            [RemoteChunk.FromInt32("TCLS", 4), RemoteChunk.FromInt32("TADR", 5), new RemoteChunk("TCMD", Hex("01 85 81 80 04 00 00"))]);
        CollectionAssert.AreEqual(
            Hex("53 32000000 07000000 534C4354 04000000 04000000 52444154 04000000 05000000 444D4354 07000000 01 85 81 80 04 00 00 75 C5"), request);

        var reply = RemoteMessage.Build(RemoteMessage.Reply, 7, [RemoteChunk.FromInt32("TSTA", 0x00070000)]);
        CollectionAssert.AreEqual(Hex("AC 17000000 07000000 41545354 04000000 00000700 08 FC"), reply);

        var parsed = RemoteMessage.Parse(request);
        Assert.IsTrue(parsed.CrcOk);
        CollectionAssert.AreEqual(new[] { "TCLS", "TADR", "TCMD" }, parsed.Chunks.Select(c => c.Tag).ToArray());
        Assert.AreEqual(5, parsed.Find("TADR")!.Int32);
    }

    [TestMethod]
    public void Sprava_SkladaSaPoCastiachASumZahodi()
    {
        var framer = new RemoteFramer();
        var raw = Hex("00 AC " + Convert.ToHexString(RemoteMessage.Build(RemoteMessage.Request, 3, [RemoteChunk.FromInt32("RCMD", 0)])));

        Assert.IsEmpty(framer.Push(raw.AsSpan(0, 8)));
        var messages = framer.Push(raw.AsSpan(8));

        Assert.HasCount(1, messages);
        Assert.IsTrue(messages[0].CrcOk);
        Assert.AreEqual(3u, messages[0].Sequence);
        Assert.AreEqual(2, framer.DiscardedBytes);
    }

    [TestMethod]
    public void TabulaElen_SamoucenieSVyrobcomZRiadiacehoInissu()
    {
        var sim = Create();

        var reply = Send(sim, 1, TableCommand(7, 5, ElenFrame.Build(5, 1, Hex("81 10 80 41 42")), 2));

        // stav: dlzka spravy tabule v hornom slove, kod 0 = v poriadku
        Assert.AreEqual(0x000B0000, Status(reply));
        var board = sim.Read(s => s.FindBoard("4-5"))!;
        Assert.AreEqual("ELEN10", board.ManufacturerKey);
        Assert.AreEqual("AB", ((ElenDisplay)board.Display).Rows[1].PlainText);
    }

    [TestMethod]
    public void TabulaElektrocasNaTejIstejLinke_InyObsah()
    {
        var sim = Create();
        Send(sim, 1, TableCommand(4, 5, ElenFrame.Build(5, 1, Hex("81 10 80 41")), 2));

        var reply = Send(sim, 2, TableCommand(5, 12, Hex("0C 19 00 16 02 00 01 00 4F 73 20 33 34 31 32 20 12 12 12 12 12 12 12 12 9A"), 1));

        Assert.AreEqual(0, Status(reply) & 0xFF);
        var board = sim.Read(s => s.FindBoard("4-12"))!;
        Assert.AreEqual("LCD1", board.ManufacturerKey);
        var row = ((CharDisplay)board.Display).Rows[2];
        Assert.AreEqual("Os 3412 ", new string(row.Layers[0].Select(c => (char)c.Code).ToArray()));
        Assert.IsInstanceOfType<ElenDisplay>(sim.Read(s => s.FindBoard("4-5"))!.Display);
    }

    [TestMethod]
    public void Poruchy_StavTabuleVOdpovedi()
    {
        var sim = Create(autoLearn: false);
        sim.SetBoard(new BoardConfig(4, 5, "ELEN") { Profile = BoardProfile.DefaultFor("ELEN") });
        sim.SetBoard(new BoardConfig(4, 12, "LCD1") { Profile = BoardProfile.DefaultFor("LCD1") });
        var elen = TableCommand(4, 5, ElenFrame.Build(5, 1, Hex("81 10 80 41")), 2);
        var lcd = TableCommand(5, 12, ElektrocasFrame.Build(12, 0, 0x40, []), 1);

        sim.SetFault("4-5", new BoardFault(FaultKind.NoReply));
        Assert.AreEqual(RemoteProtocol.TableNoAnswer, Status(Send(sim, 1, elen)) & 0xFF);
        sim.SetFault("4-5", new BoardFault(FaultKind.Nak));
        Assert.AreEqual(RemoteProtocol.TableRejected, Status(Send(sim, 2, elen)) & 0xFF);
        sim.SetFault("4-12", new BoardFault(FaultKind.Status, 0, 0x40));
        Assert.AreEqual(0x15, Status(Send(sim, 3, lcd)) & 0xFF);

        // tabula, ktora na adrese nie je, neodpovie
        Assert.AreEqual(RemoteProtocol.TableNoAnswer, Status(Send(sim, 4, TableCommand(4, 9, ElenFrame.Build(9, 1, Hex("81 10 80 41")), 2))) & 0xFF);
    }

    [TestMethod]
    public void InyVyrobcaAleboRodina_NavrhNaPrepnutie()
    {
        var sim = Create(autoLearn: false);
        sim.SetBoard(new BoardConfig(4, 12, "ELEN") { Profile = BoardProfile.DefaultFor("ELEN") });

        var reply = Send(sim, 1, TableCommand(12, 12, ElektrocasFrame.Build(12, 0, 0x40, []), 1));

        // tabula ELEN spravu Elektrocasu nedostane, ponukne sa prepnutie na ERP
        Assert.AreEqual(RemoteProtocol.TableNoAnswer, Status(reply) & 0xFF);
        var mismatch = sim.Read(s => s.FindBoard("4-12"))!.Mismatch;
        Assert.AreEqual("remote.manufacturer", mismatch?.Evidence);
        CollectionAssert.AreEqual(new[] { "ERP" }, mismatch!.Suggested.ToArray());
    }

    [TestMethod]
    public void HromadnyPrikaz_VsetkyTabuleRodinyBezOdpovede()
    {
        var sim = Create(autoLearn: false);
        sim.SetBoard(new BoardConfig(4, 5, "ELEN") { Profile = BoardProfile.DefaultFor("ELEN") });
        sim.SetBoard(new BoardConfig(4, 6, "ELEN16") { Profile = BoardProfile.DefaultFor("ELEN16") });
        sim.SetBoard(new BoardConfig(4, 12, "LCD1") { Profile = BoardProfile.DefaultFor("LCD1") });

        var reply = Send(sim, 1, TableCommand(4, -1, ElenFrame.Build(0, 4, Hex("80 85")), 2));

        Assert.AreEqual(0, Status(reply) & 0xFF);
        Assert.AreEqual(5, ((ElenDisplay)sim.Read(s => s.FindBoard("4-5"))!.Display).Brightness);
        Assert.AreEqual(5, ((ElenDisplay)sim.Read(s => s.FindBoard("4-6"))!.Display).Brightness);
    }

    [TestMethod]
    public void NepodporovanyVyrobca_VzdialenyNemaLinku()
    {
        var sim = Create();

        var reply = Send(sim, 1, TableCommand(10, 130, Hex("82 00 03 41 42 43"), 0));

        Assert.AreEqual(RemoteProtocol.TableNoLine, Status(reply) & 0xFF);
        Assert.IsFalse(sim.Read(s => s.Boards.Any()));
    }

    [TestMethod]
    public void Fronty_HlaseniePrijateAVypnutieSaPamata()
    {
        var sim = Create();
        var name = Encoding.Latin1.GetBytes("Nastupiste 1");

        // jedina fronta bez nazvu: necinna
        var idle = Send(sim, 1, RemoteChunk.FromInt32("RCMD", 0));
        Assert.AreEqual(1, idle.Find("RSTA")!.Int32);
        Assert.IsNull(idle.Find("QNAM"));
        Assert.IsNull(idle.Find("TSTA"));

        // hlasenie: prijate (0x100), potom dalsi dotaz bez neho = dohrate
        var report = Send(sim, 2, new RemoteChunk("QNAM", name), RemoteChunk.FromInt32("RCMD", 0), new RemoteChunk("RWAV", "a1,0,0"u8.ToArray()),
            new RemoteChunk("RTIT", "Os 3412"u8.ToArray()));
        CollectionAssert.AreEqual(name, report.Find("QNAM")!.Data);
        Assert.AreEqual(0x103, report.Find("RSTA")!.Int32);
        Assert.AreEqual(0x401, Send(sim, 3, new RemoteChunk("QNAM", name), RemoteChunk.FromInt32("RCMD", 1)).Find("RSTA")!.Int32);

        // vypnutie fronty: ozvena ziadosti, stav 2 ostane aj po nej
        Assert.AreEqual(0x30000002, Send(sim, 4, new RemoteChunk("QNAM", name), RemoteChunk.FromInt32("RCMD", 0x30000000)).Find("RSTA")!.Int32);
        Assert.AreEqual(2, Send(sim, 5, new RemoteChunk("QNAM", name), RemoteChunk.FromInt32("RCMD", 0)).Find("RSTA")!.Int32);
        Assert.AreEqual(0x10000001, Send(sim, 6, new RemoteChunk("QNAM", name), RemoteChunk.FromInt32("RCMD", 0x10000000)).Find("RSTA")!.Int32);
    }

    [TestMethod]
    public void ImportStanice_TabuleRoznychRodinNaLinkeVzdialenehoInissu()
    {
        var sim = new Simulator();

        var result = sim.ImportStation(new StationImport
        {
            Lines = [new StationLine(4, 128, 8090) { Name = "Driver1" }],
            Boards = [new StationBoard(4, 12, "ELEN16"), new StationBoard(4, 20, "LCD1"), new StationBoard(4, 30, "APEL")]
        });

        Assert.AreEqual(2, result.BoardsAdded);
        CollectionAssert.AreEqual(new[] { "4-30" }, result.SkippedBoards.ToArray());
        Assert.IsInstanceOfType<ElenDisplay>(sim.Read(s => s.FindBoard("4-12"))!.Display);
        Assert.IsInstanceOfType<CharDisplay>(sim.Read(s => s.FindBoard("4-20"))!.Display);

        // zmena vyrobcu na inu rodinu da tabuli novy obsah
        sim.SetBoard(new BoardConfig(4, 12, "ERP") { Profile = BoardProfile.DefaultFor("ERP") });
        Assert.IsInstanceOfType<CharDisplay>(sim.Read(s => s.FindBoard("4-12"))!.Display);
    }

    [TestMethod]
    public void ZlySucet_BezOdpovede()
    {
        var sim = Create();
        var raw = RemoteMessage.Build(RemoteMessage.Request, 1, [RemoteChunk.FromInt32("RCMD", 0)]);
        raw[^1] ^= 0xFF;

        Assert.IsEmpty(sim.Receive(4, raw));
        Assert.AreEqual(1, sim.Read(s => s.Lines.First().Stats.BadFrames));
    }

    [TestMethod]
    public void Zrkadlo_DostaneDokoncenuSpravuTabule()
    {
        var sim = Create();
        var mirrored = new List<byte[]>();
        sim.Traffic += (_, data) => mirrored.Add(data);
        var frame = ElenFrame.Build(5, 1, Hex("81 10 80 41"));

        Send(sim, 1, TableCommand(4, 5, frame, 2));
        Send(sim, 2, RemoteChunk.FromInt32("RCMD", 0));

        Assert.HasCount(1, mirrored);
        CollectionAssert.AreEqual(frame, mirrored[0]);
    }
}
