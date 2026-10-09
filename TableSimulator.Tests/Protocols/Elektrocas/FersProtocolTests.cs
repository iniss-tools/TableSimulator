using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Protocols.Remote;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.Elektrocas;

/// <summary>
/// Listove tabule FERS (TableClass 3, ramec Elektrocasu): priklady z docs/iniss/protokoly/ers-fers.mdx, otacanie listov
/// (na dotaz 01 tabula mlci), texty listov zo zoznamov profilu, import stanice a FERS za vzdialenym INISSom.
/// </summary>
[TestClass]
public class FersProtocolTests
{
    private static readonly FlapProfile Departures = new()
    {
        Modules = [new FlapModule(0, 0, 1, "Ciel"), new FlapModule(0, 1, 1, "Druh"), new FlapModule(0, 2, 1, "Hodiny"), new FlapModule(0, 3, 1, "Minuty"),
            new FlapModule(0, 4, 1, "Minuty")],
        Lists = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["Ciel"] = new Dictionary<string, string> { ["A"] = "BRATISLAVA", ["B"] = "*BRATISLAVA", ["0"] = "" },
            ["Druh"] = new Dictionary<string, string> { ["R"] = "R", ["O"] = "Os" },
            ["Hodiny"] = new Dictionary<string, string> { ["G"] = "6" },
            ["Minuty"] = new Dictionary<string, string> { ["0"] = "0", ["1"] = "1", ["5"] = "5" }
        }
    };

    private static (Simulator Sim, ManualTime Time) Create(FlapProfile? flaps = null)
    {
        var time = new ManualTime();
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(3, 3, "TCP://127.0.0.1:47003") { AutoLearn = false, Manufacturer = "FERS" });
        sim.SetBoard(new BoardConfig(3, 7, "FERS") { Profile = BoardProfile.DefaultFor("FERS") with { RotationMs = 2000, Flaps = flaps } });
        return (sim, time);
    }

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(3, data).SelectMany(r => r.Bytes).ToArray();

    private static SimBoard Board(Simulator sim) => sim.Read(s => s.FindBoard("3-7"))!;

    [TestMethod]
    public void Zapis_PrikladZDokumentacieAOtacanie()
    {
        var (sim, time) = Create();

        CollectionAssert.AreEqual(Hex("00 06 07 70 00 7D"), Replies(sim, Hex("07 0D 00 10 01 02 52 20 31 32 33 20 4F")));
        var display = (FlapDisplay)Board(sim).Display;
        Assert.AreEqual("  R 123 ", display.Rows[1]);

        // kym sa listy tocia, na dotaz 01 tabula neodpovie
        Assert.IsEmpty(Replies(sim, Hex("07 05 00 01 0D")));
        time.Advance(TimeSpan.FromSeconds(2));
        CollectionAssert.AreEqual(Hex("00 06 07 70 00 7D"), Replies(sim, Hex("07 05 00 01 0D")));

        // zmazanie - potvrdi a zase otaca
        CollectionAssert.AreEqual(Hex("00 06 07 70 00 7D"), Replies(sim, Hex("07 05 00 40 4C")));
        Assert.IsEmpty(display.Rows);
        Assert.IsEmpty(Replies(sim, Hex("07 05 00 01 0D")));
    }

    [TestMethod]
    public void Porucha_ChybaOtacaniaVStave()
    {
        var (sim, _) = Create();
        sim.SetFault("3-7", new BoardFault(FaultKind.Status, 0, 0x20));

        var reply = Replies(sim, Hex("07 05 00 01 0D"));

        CollectionAssert.AreEqual(ElektrocasFrame.Build(0, 7, ElektrocasProtocol.ReplyStatus, [0x20]), reply);
    }

    [TestMethod]
    public void TextyListov_ZoZoznamovProfilu()
    {
        var (sim, _) = Create(Departures);

        Replies(sim, ElektrocasFrame.Build(7, 0, 0x10, [0, 0, .. "BRG15"u8]));
        Replies(sim, ElektrocasFrame.Build(7, 0, 0x10, [1, 0, .. "AOX"u8]));

        // cerveny list zostava v texte s *, kod bez textu v zozname (X) sa ukaze, ako prisiel
        CollectionAssert.AreEqual(new[] { "*BRATISLAVA | R | 6 | 1 | 5", "BRATISLAVA | Os | X", "", "" }, BoardText.Rows(Board(sim)).ToArray());
    }

    [TestMethod]
    public void ImportStanice_ProfilZoZoznamovAPrevzatieListov()
    {
        var sim = new Simulator();
        sim.ImportStation(new StationImport
        {
            Lines = [new StationLine(3, 3, 47003)],
            Boards = [new StationBoard(3, 7, "FERS") { Flaps = Departures, FlapRecords = 10 }]
        });

        var board = Board(sim);
        Assert.IsInstanceOfType<FlapDisplay>(board.Display);
        Assert.AreEqual(10, board.Profile.Rows);
        Assert.AreEqual(5 * 8, board.Profile.Width);
        Assert.AreSame(Departures, board.Profile.Flaps);

        // farby listov nastavene v simulatore
        sim.SetBoard(new BoardConfig(3, 7, "FERS")
        {
            Profile = board.Profile with
            {
                Flaps = Departures with
                {
                    Colors = new Dictionary<string, IReadOnlyDictionary<string, string>>
                    {
                        ["Druh"] = new Dictionary<string, string> { ["O"] = FlapColors.Yellow, ["Z"] = FlapColors.White },
                        ["Stary"] = new Dictionary<string, string> { ["A"] = FlapColors.White }
                    }
                }
            }
        });

        // tabula uz je - zoznamy listov sa prevezmu znova (su to data stanice), farby existujucich listov ostanu
        var updated = Departures with { LinesPerRecord = 2 };
        sim.ImportStation(new StationImport { Lines = [new StationLine(3, 3, 47003)], Boards = [new StationBoard(3, 7, "FERS") { Flaps = updated }] });
        var flaps = Board(sim).Profile.Flaps!;
        Assert.AreEqual(2, flaps.LinesPerRecord);
        Assert.AreSame(Departures.Lists, flaps.Lists);
        Assert.AreEqual(FlapColors.Yellow, flaps.Colors["Druh"]["O"]);
        Assert.HasCount(1, flaps.Colors["Druh"]);
        Assert.IsFalse(flaps.Colors.ContainsKey("Stary"));
    }

    [TestMethod]
    public void VzdialenyIniss_FersAj_Ers()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(4, 128, "TCP://127.0.0.1:8090") { AutoLearn = true });
        var fers = ElektrocasFrame.Build(7, 0, 0x10, [0, 0, .. "AB"u8]);
        fers[^1] = 0;

        var reply = RemoteMessage.Parse(sim.Receive(4, RemoteMessage.Build(RemoteMessage.Request, 1,
            [RemoteChunk.FromInt32("TCLS", 3), RemoteChunk.FromInt32("TADR", 7), new RemoteChunk("TCMD", fers)]))[0].Bytes);
        var ers = RemoteMessage.Parse(sim.Receive(4, RemoteMessage.Build(RemoteMessage.Request, 2,
            [RemoteChunk.FromInt32("TCLS", 2), RemoteChunk.FromInt32("TADR", 5), new RemoteChunk("TCMD", Core.Protocols.Ers.ErsFrame.Build(5, 1, "12"))]))[0].Bytes);

        Assert.AreEqual(0, reply.Find("TSTA")!.Int32 & 0xFF);
        Assert.AreEqual(0, ers.Find("TSTA")!.Int32 & 0xFF);
        Assert.AreEqual("AB", ((FlapDisplay)sim.Read(s => s.FindBoard("4-7"))!.Display).Rows[0]);
        Assert.AreEqual("ERS", sim.Read(s => s.FindBoard("4-5"))!.ManufacturerKey);
        Assert.AreEqual("12", ((FlapDisplay)sim.Read(s => s.FindBoard("4-5"))!.Display).Rows[0]);
    }
}
