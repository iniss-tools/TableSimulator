using System.Text;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;
using TableSimulator.Core.Boards.Announcements;
using TableSimulator.Core.Protocols.AkSignal;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Tests.Protocols.AkSignal;

/// <summary>
/// Ustredna AK Signal UH s protokolom AKSIGNALUH (TableClass 132): priklady z docs/iniss2/protokoly/aksignal.mdx,
/// svetlo, hlasenie s dlzkou podla zvukov, zastavenie, zosuladenie banky, stavy ustredne a poruchy.
/// </summary>
[TestClass]
public class AkSignalUhProtocolTests
{
    private const int Line = 7;

    private static readonly Dictionary<string, int> AudioKeys = new(StringComparer.Ordinal)
        { ["run"] = 1, ["hiprt"] = 2, ["cancl"] = 4, ["pwr"] = 8, ["sup"] = 0x10, ["comerr"] = 0x20, ["sync"] = 0x40 };

    private static Simulator Create(ManualTime time, TwilightMode sensor = TwilightMode.Dark, AkSignalConditions? conditions = null)
    {
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(Line, 132, "TCP://127.0.0.1:47007") { AutoLearn = false, Manufacturer = AkSignalUhProtocol.Manufacturer });
        SetProfile(sim, sensor, conditions ?? new AkSignalConditions());
        return sim;
    }

    private static void SetProfile(Simulator sim, TwilightMode sensor, AkSignalConditions conditions) =>
        sim.SetBoard(new BoardConfig(Line, AkSignalUhProtocol.Address, AkSignalUhProtocol.Manufacturer)
        {
            Profile = new BoardProfile
            {
                AkSignal = new AkSignalProfile { Sensor = new TwilightSensor { Mode = sensor }, Conditions = conditions, SoundSeconds = 2, SyncSeconds = 3 }
            }
        });

    private static string Send(Simulator sim, string text) =>
        Encoding.UTF8.GetString(sim.Receive(Line, Encoding.UTF8.GetBytes(text)).SelectMany(r => r.Bytes).ToArray());

    private static AkSignalDisplay Display(Simulator sim) => (AkSignalDisplay)sim.Read(s => s.FindBoard("7-0"))!.Display;

    [TestMethod]
    public void Spravy_PrikladyZDokumentacie()
    {
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("<aud|ch=1|stop>"), AkSignalUhFrame.Build("aud", "ch=1", "stop"));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("<#aud|st=run,pwr>"), AkSignalUhFrame.Answer("aud", "run,pwr"));

        var sim = Create(new ManualTime());
        Assert.AreEqual("<#aud|st=run>", Send(sim, "<aud|ch=1|CZ/vlak/1234*CZ/kolaj/2>"));
        Assert.AreEqual("<#light|st=lit,dark,ok,on>", Send(sim, "<light|lit=1>"));
    }

    [TestMethod]
    public void Svetlo_AkoServer()
    {
        var sim = Create(new ManualTime());
        var server = new IniSrvAkSignal(sim, Line, AkSignalUhProtocol.Address, true);

        Assert.AreEqual(IniSrvAkSignal.Twilight, server.CheckLight());
        server.RequireAuto = true;
        Assert.AreEqual(IniSrvAkSignal.ModeAuto | IniSrvAkSignal.LightOn | IniSrvAkSignal.Twilight, server.CheckLight());
        Assert.AreEqual(IniSrvAkSignal.ModeForce | IniSrvAkSignal.LightOn | IniSrvAkSignal.Twilight, server.CheckLight(IniSrvAkSignal.ModeForce));
        Assert.AreEqual(0, server.StateErrors);
    }

    [TestMethod]
    public void Svetlo_PoruchaBezKlucov()
    {
        // bez ok a bez inych klucov ide prazdny stav - 0 by server pri svetle odmietol ako CHYBA_STAVU
        var sim = Create(new ManualTime(), TwilightMode.Light, new AkSignalConditions { LightsFault = true });
        Assert.AreEqual("<#light|st=>", Send(sim, "<light>"));

        var server = new IniSrvAkSignal(sim, Line, AkSignalUhProtocol.Address, true);
        Assert.AreEqual(IniSrvAkSignal.Failure, server.CheckLight());
        Assert.AreEqual(0, server.StateErrors);
    }

    [TestMethod]
    public void Hlasenie_DlzkaPodlaZvukov()
    {
        var time = new ManualTime();
        var sim = Create(time);
        var server = new IniSrvAkSignal(sim, Line, AkSignalUhProtocol.Address, true);

        // server po starte zastavi kanal a zosuladi banky
        Assert.AreEqual(0, server.SendUh("aud", "ch=1|stop", AudioKeys));
        Assert.AreEqual(AudioKeys["sync"], server.SendUh("sync", "CZ=0123ABCD|SK=4567EF01", AudioKeys));
        CollectionAssert.AreEquivalent(new Dictionary<string, string> { ["CZ"] = "0123ABCD", ["SK"] = "4567EF01" }, Display(sim).Banks.ToDictionary());
        time.Advance(TimeSpan.FromSeconds(3));
        Assert.AreEqual(0, server.Audio());

        // dva zvuky po 2 s: hra 4 s
        Assert.AreEqual(AudioKeys["run"], server.SendUh("aud", "ch=1|CZ/vlak/1234*CZ/kolaj/2", AudioKeys));
        CollectionAssert.AreEqual(new[] { "CZ/vlak/1234", "CZ/kolaj/2" }, Display(sim).Playing(1)!.Sounds.ToArray());
        CollectionAssert.AreEqual(new[] { "LIGHT DARK OFF", "CH1 CZ/vlak/1234*CZ/kolaj/2" }, BoardText.Rows(sim.Read(s => s.FindBoard("7-0"))!).ToArray());
        time.Advance(TimeSpan.FromSeconds(3.5));
        Assert.AreEqual(AudioKeys["run"], server.Audio());
        time.Advance(TimeSpan.FromSeconds(0.5));
        Assert.AreEqual(0, server.Audio());

        var done = Display(sim).Announcements.Single();
        Assert.AreEqual(AnnouncementResult.Completed, done.Result);
        Assert.AreEqual(TimeSpan.FromSeconds(4), done.EndedAt - done.StartedAt);
    }

    [TestMethod]
    public void Hlasenie_DlzkaZBankyServera()
    {
        var time = new ManualTime();
        var sim = Create(time);
        sim.SetEnvironment(new SimEnvironment { SoundLength = sound => sound == "CZ/gong" ? TimeSpan.FromSeconds(5) : null });

        Send(sim, "<aud|ch=2|CZ/gong*CZ/neznamy>");
        Assert.AreEqual(TimeSpan.FromSeconds(7), Display(sim).Playing(2)!.Duration, "neznamy zvuk ma dlzku z profilu");

        // koniec hlasenia zisti aj tik bez dotazu servera
        time.Advance(TimeSpan.FromSeconds(7));
        Assert.AreEqual(1, sim.Tick());
        Assert.IsNull(Display(sim).Playing(2));
    }

    [TestMethod]
    public void Hlasenie_ZastavenieAKanaly()
    {
        var time = new ManualTime();
        var sim = Create(time);

        Send(sim, "<aud|ch=1|CZ/a*CZ/b>");
        Send(sim, "<aud|ch=2|SK/c>");
        Assert.AreEqual("<#aud|st=run>", Send(sim, "<aud>"));
        Assert.AreEqual("<#aud|st=0>", Send(sim, "<aud|ch=1|stop>"));
        Assert.AreEqual("<#aud|st=run>", Send(sim, "<aud>"), "kanal 2 hra dalej");
        Assert.AreEqual(AnnouncementResult.Stopped, Display(sim).Announcements[0].Result);

        // neplatny kanal ustredna nehra
        Send(sim, "<aud|ch=3|CZ/a>");
        Assert.IsNull(Display(sim).Playing(1));
        Assert.HasCount(2, Display(sim).Announcements);
    }

    [TestMethod]
    public void Hlasenie_VyssiaPrioritaANapajanie()
    {
        var time = new ManualTime();
        var sim = Create(time);

        Send(sim, "<aud|ch=1|CZ/a>");
        SetProfile(sim, TwilightMode.Dark, new AkSignalConditions { HighPriority = true });
        Assert.AreEqual("<#aud|st=hiprt,cancl>", Send(sim, "<aud>"));
        Assert.AreEqual(AnnouncementResult.Cancelled, Display(sim).Announcements.Single().Result);

        // kym hra hlasenie s vyssou prioritou, ustredna nehra
        Assert.AreEqual("<#aud|st=hiprt>", Send(sim, "<aud|ch=1|CZ/a>"));

        SetProfile(sim, TwilightMode.Dark, new AkSignalConditions { AmpSupplyMissing = true, OutputPowerError = true, AmpCommError = true, BankNotSynced = true });
        Assert.AreEqual("<#aud|st=pwr,sup,comerr,sync>", Send(sim, "<aud|ch=1|CZ/a>"));
        Assert.IsNull(Display(sim).Playing(1));
    }

    [TestMethod]
    public void Chyby_SumNeznamyPrikazPoruchy()
    {
        var sim = Create(new ManualTime());

        // sum pred spravou a nedokoncena sprava pred novou sa zahodia
        Assert.AreEqual("<#light|st=dark,ok>", Send(sim, "xx<aud|ch<light>"));
        Assert.AreEqual(9L, sim.Read(s => s.FindLine(Line)!.Stats.Discarded));

        Assert.AreEqual("", Send(sim, "<reset>"));

        var server = new IniSrvAkSignal(sim, Line, AkSignalUhProtocol.Address, true);
        sim.SetFault("7-0", new BoardFault(FaultKind.Nak, 0, 0));
        Assert.AreEqual(IniSrvAkSignal.NotConnected, server.CheckLight());
        sim.SetFault("7-0", new BoardFault(FaultKind.Garbage, 0, 0));
        Assert.IsNull(server.Audio());

        // zvoleny stav ako bity klucov
        sim.SetFault("7-0", new BoardFault(FaultKind.Status, 0, 0x12));
        Assert.AreEqual("<#aud|st=hiprt,sup>", Send(sim, "<aud>"));
        Assert.AreEqual("<#light|st=man,on>", Send(sim, "<light>"));
    }
}
