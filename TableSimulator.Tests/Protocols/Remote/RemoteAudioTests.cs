using TableSimulator.Application.Devices;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Remote;
using TableSimulator.Core.Simulation;
using ToolsCore.Iniss.Entities;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Tests.Protocols.Remote;

/// <summary>
/// Rozhlas vzdialeneho INISSu (trieda 128): zoznam RWAV s pauzami, hlasenie hra podla dlzky nahravok, riadiaci ho
/// zastavi alebo ho obsluha prerusi (remote break), porucha zariadenia = vzdialeny INISS mlci (remote timeout), dlzky
/// z banky INISS 3.39 a spinanie zosilnovaca signalom DTR.
/// </summary>
[TestClass]
public class RemoteAudioTests
{
    private const int Line = 4;

    private static readonly TimeSpan Sound = TimeSpan.FromSeconds(2);

    private static Simulator Create(ManualTime time)
    {
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(Line, 128, "TCP://127.0.0.1:8090") { AutoLearn = false });
        sim.SetEnvironment(new SimEnvironment { SoundLength = name => name.StartsWith("SK/", StringComparison.Ordinal) ? Sound : null });
        return sim;
    }

    private static RemoteChunk[] Request(int command, string? recordings = null, string? title = null, string? circuit = null)
    {
        var chunks = new List<RemoteChunk> { RemoteChunk.FromInt32("RCMD", command) };
        if (recordings is not null)
            chunks.Add(new RemoteChunk("RWAV", System.Text.Encoding.Latin1.GetBytes(recordings)));
        if (title is not null)
            chunks.Add(new RemoteChunk("RTIT", System.Text.Encoding.Latin1.GetBytes(title)));
        if (circuit is not null)
            chunks.Add(new RemoteChunk("LINE", System.Text.Encoding.Latin1.GetBytes(circuit)));
        return [.. chunks];
    }

    /// <summary>Stav fronty z odpovede; null = vzdialeny INISS neodpovedal.</summary>
    private static int? Send(Simulator sim, uint seq, params RemoteChunk[] chunks)
    {
        var replies = sim.Receive(Line, RemoteMessage.Build(RemoteMessage.Request, seq, chunks));
        return replies.Count == 0 ? null : RemoteMessage.Parse(replies[0].Bytes).Find("RSTA")!.Int32;
    }

    private static RemoteAudioDisplay Audio(Simulator sim) => (RemoteAudioDisplay)sim.Read(s => s.FindBoard("4-0"))!.Display;

    [TestMethod]
    public void Zoznam_TrojiceSPauzamiADlzka()
    {
        var sounds = RemoteSound.Parse("SK/R1/9900100,0,0, SK/VLAK/prich ,100,250,CZ/x,y");

        Assert.AreEqual(new RemoteSound("SK/R1/9900100", 0, 0), sounds[0]);
        Assert.AreEqual(new RemoteSound("SK/VLAK/prich", 100, 250), sounds[1]);
        Assert.AreEqual(new RemoteSound("CZ/x", 0, 0), sounds[2], "necitatelne cislo = 0");
        var duration = RemoteSound.Duration(sounds, name => name.StartsWith("SK/", StringComparison.Ordinal) ? Sound : null, TimeSpan.FromSeconds(1.5));
        Assert.AreEqual(TimeSpan.FromMilliseconds(2000 + 2350 + 1500), duration);
    }

    [TestMethod]
    public void Hlasenie_HraPodlaDlzkyADohraje()
    {
        var time = new ManualTime();
        var sim = Create(time);

        // prijate: zariadenie vznikne aj bez samoucenia, hlasenie trva dve nahravky
        Assert.AreEqual(0x103, Send(sim, 1, Request(0, "SK/R1/9900100,0,0,SK/VLAK/prich,0,500", "Os 3412", "Nastupiste")));
        var playing = Audio(sim).Queues.Single().Playing!;
        Assert.AreEqual((TimeSpan.FromMilliseconds(4500), "Os 3412", "", "Nastupiste"), (playing.Duration!.Value, playing.Title, playing.Queue, playing.Circuit));
        CollectionAssert.AreEqual(new[] { "SK/R1/9900100", "SK/VLAK/prich" }, playing.Sounds.ToArray());

        // prevzate - kym hra, stav ma 0x100
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(0x503, Send(sim, 2, Request(1)));

        // dohralo: tik ho ukonci, dalsia odpoved bez 0x100 - riadiaci zapise OK
        time.Advance(TimeSpan.FromSeconds(4));
        Assert.AreEqual(1, sim.Tick());
        Assert.AreEqual(0x401, Send(sim, 3, Request(1)));
        var done = sim.TakeDeviceHistory().Announcements.Single();
        Assert.AreEqual((Line, 0, AnnouncementResult.Completed), (done.Line, done.Address, done.Announcement.Result));
        Assert.AreEqual(time.GetUtcNow() - TimeSpan.FromMilliseconds(500), done.Announcement.EndedAt);
    }

    [TestMethod]
    public void Hlasenie_RiadiaciZastaviAleboVypneFrontu()
    {
        var time = new ManualTime();
        var sim = Create(time);
        Send(sim, 1, Request(0, "SK/R1/1,0,0"));
        Send(sim, 2, Request(1));

        // prevzate hlasenie bez bitu "pokracovat" = zastavene
        Assert.AreEqual(1, Send(sim, 3, Request(0)));
        Assert.AreEqual(AnnouncementResult.Stopped, Audio(sim).Announcements.Single().Result);

        // vypnutie fronty zastavi hrajuce hlasenie, nove neprevezme
        Send(sim, 4, Request(0, "SK/R1/2,0,0"));
        Assert.AreEqual(0x30000002, Send(sim, 5, Request(0x30000000)));
        Assert.AreEqual(2, Send(sim, 6, Request(0, "SK/R1/3,0,0")));
        CollectionAssert.AreEqual(new[] { AnnouncementResult.Stopped, AnnouncementResult.Stopped }, Audio(sim).Announcements.Select(a => a.Result).ToArray());
    }

    [TestMethod]
    public void Prerusenie_OdpovedBezStavuFronty()
    {
        var time = new ManualTime();
        var sim = Create(time);
        Send(sim, 1, Request(0, "SK/R1/1,0,0"));
        Send(sim, 2, Request(1));

        Assert.AreEqual(BreakResult.Broken, sim.BreakAnnouncement("4-0", ""));
        Assert.AreEqual(AnnouncementResult.Cancelled, Audio(sim).Announcements.Single().Result);
        Assert.AreEqual(BreakResult.NothingPlaying, sim.BreakAnnouncement("4-0", null));

        // raz odpoved s nulou (riadiaci zapise remote break), potom zase volna fronta
        Assert.AreEqual(0, Send(sim, 3, Request(1)));
        Assert.IsTrue(sim.LogSince(0).Any(f => f.Info.Kind == "remote.break"));
        Assert.AreEqual(1, Send(sim, 4, Request(0)));
        sim.SetBoard(new BoardConfig(Line, 5, "ELEN"));
        Assert.AreEqual(BreakResult.NotRemoteAudio, sim.BreakAnnouncement("4-5", null));
        Assert.AreEqual(BreakResult.NotFound, sim.BreakAnnouncement("4-9", null));
    }

    [TestMethod]
    public void PoruchaZariadenia_VzdialenyInissMlciAleboMeska()
    {
        var time = new ManualTime();
        var sim = Create(time);
        Send(sim, 1, Request(0, "SK/R1/1,0,0"));

        sim.SetFault("4-0", new BoardFault(FaultKind.NoReply));
        Assert.IsNull(Send(sim, 2, Request(1)), "riadiaci INISS zapise remote timeout");
        Assert.IsTrue(sim.LogSince(0).Any(f => f.Info.Kind == "remote.silent"));

        sim.SetFault("4-0", new BoardFault(FaultKind.Delay, 700));
        var replies = sim.Receive(Line, RemoteMessage.Build(RemoteMessage.Request, 3, Request(1)));
        Assert.AreEqual(TimeSpan.FromMilliseconds(700), replies.Single().Delay);
    }

    [TestMethod]
    public void Banka339_DlzkyPodlaKlucovZoZoznamu()
    {
        var folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        var bank = Path.Combine(folder, "RAWBANK") + Path.DirectorySeparatorChar;
        try
        {
            Directory.CreateDirectory(Path.Combine(bank, "SK"));
            var language = new FyzLanguage("SK", "Slovenčina", "FYZZVUK.DAT", "SK\\");
            var group = new FyzGroup(language, "VlakNum", "Číslovky", "R1\\");
            group.Sounds.Add(new FyzSound(group, "9900100", "9900100", "9900100.EWA", "", "Lipovec", 1840));
            group.Sounds.Add(new FyzSound(group, "prazdna", "prazdna", "PRAZDNA.EWA", "", "", 0));
            language.Groups = [group];
            RawBankParser.WriteFyzBankFile(bank, [language]);
            RawBankParser.WriteFyzZvukFile(bank, language);
            File.WriteAllBytes(Path.Combine(folder, "INISS.exe"), []);

            Assert.AreEqual(Path.Combine(folder, "RAWBANK"), Iniss339SoundLengths.BankOf(Path.Combine(folder, "INISS.exe")));
            var lengths = new Iniss339SoundLengths(Path.Combine(folder, "RAWBANK"));
            Assert.AreEqual(TimeSpan.FromMilliseconds(1840), lengths.Length("sk/vlaknum/9900100"), "kluce bez ohladu na velkost pismen");
            Assert.IsNull(lengths.Length("SK/Číslovky/9900100"), "nazov skupiny sa nepouziva");
            Assert.IsNull(lengths.Length("SK/VlakNum/prazdna"), "nulova dlzka a chybajuci subor");
            Assert.IsNull(Iniss339SoundLengths.BankOf(Path.Combine(Path.GetTempPath(), "nie", "INISS.exe")));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    [TestMethod]
    public void Dtr_ZopnutieZosilnovacaAZaznam()
    {
        var time = new ManualTime();
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(9, Simulator.ModemSignalClass, "COM7:9600") { AutoLearn = false });
        Assert.IsTrue(sim.WatchesModemSignal(9));
        Assert.IsFalse(sim.WatchesModemSignal(Line));
        Assert.IsFalse(sim.SetModemSignal(9, false), "vypnuty bez zariadenia - nic");

        Assert.IsTrue(sim.SetModemSignal(9, true));
        Assert.IsFalse(sim.SetModemSignal(9, true));
        var amplifier = (AmplifierDisplay)sim.Read(s => s.FindBoard("9-0"))!.Display;
        Assert.AreEqual((1, 1, time.GetUtcNow()), (amplifier.Band, amplifier.Switches, amplifier.OnSince!.Value));

        time.Advance(TimeSpan.FromSeconds(3));
        Assert.IsTrue(sim.SetModemSignal(9, false));
        Assert.AreEqual(0, amplifier.Band);
        CollectionAssert.AreEqual(new[] { "dtr.on", "dtr.off" }, sim.LogSince(0).Select(f => f.Info.Kind).ToArray());

        // linka DTR len na seriovom porte
        var validator = new TableSimulator.Application.Lines.SetLineCommandValidator();
        var tcp = validator.Validate(new TableSimulator.Application.Lines.SetLineCommand(9,
            new Contracts.Lines.SetLineRequest { TableClass = Simulator.ModemSignalClass, Endpoint = "TCP://0.0.0.0:47009" }));
        Assert.AreEqual("line.dtrSerial", tcp.Errors.Single().ErrorCode);
        Assert.IsTrue(validator.Validate(new TableSimulator.Application.Lines.SetLineCommand(9,
            new Contracts.Lines.SetLineRequest { TableClass = Simulator.ModemSignalClass, Endpoint = "COM7:9600" })).IsValid);
    }
}
