using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.AkSignal;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Protocols.AkSignal;

/// <summary>
/// Ustredna AK Signal s protokolom AKSIGNAL (TableClass 131): priklady z docs/iniss2/protokoly/aksignal.mdx, riadenie
/// svetla ako ho robi server, rozhlas, stavy ustredne, zly sucet a poruchy.
/// </summary>
[TestClass]
public class AkSignalProtocolTests
{
    private const int Line = 6, Address = 5;

    private static Simulator Create(TwilightMode sensor = TwilightMode.Light, AkSignalConditions? conditions = null, ManualTime? time = null)
    {
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(Line, 131, "TCP://127.0.0.1:47006") { AutoLearn = false, Manufacturer = AkSignalProtocol.Manufacturer });
        sim.SetBoard(new BoardConfig(Line, Address, AkSignalProtocol.Manufacturer)
        {
            Profile = new BoardProfile { AkSignal = new AkSignalProfile { Sensor = new TwilightSensor { Mode = sensor }, Conditions = conditions ?? new AkSignalConditions() } }
        });
        return sim;
    }

    private static byte[] Replies(Simulator sim, byte[] data) => sim.Receive(Line, data).SelectMany(r => r.Bytes).ToArray();

    private static AkSignalDisplay Display(Simulator sim) => (AkSignalDisplay)sim.Read(s => s.FindBoard("6-5"))!.Display;

    private static void SetConditions(Simulator sim, AkSignalConditions conditions, TwilightMode sensor = TwilightMode.Light) =>
        sim.SetBoard(new BoardConfig(Line, Address, AkSignalProtocol.Manufacturer)
        {
            Profile = new BoardProfile { AkSignal = new AkSignalProfile { Sensor = new TwilightSensor { Mode = sensor }, Conditions = conditions } }
        });

    [TestMethod]
    public void Spravy_PrikladyZDokumentacie()
    {
        CollectionAssert.AreEqual(Hex("05 04 10 19"), AkSignalFrame.Build(5, AkSignalFrame.GetAudio));
        CollectionAssert.AreEqual(Hex("05 04 11 1A"), AkSignalFrame.Build(5, AkSignalFrame.ClearAudio));
        CollectionAssert.AreEqual(Hex("05 06 12 01 00 1E"), AkSignalFrame.Build(5, AkSignalFrame.SetAudio, 1));
        CollectionAssert.AreEqual(Hex("05 06 12 FF FF 1B"), AkSignalFrame.Build(5, AkSignalFrame.SetAudio));
        CollectionAssert.AreEqual(Hex("00 05 10 11 26"), AkSignalFrame.Answer(AkSignalFrame.GetAudio, 0x11));
        CollectionAssert.AreEqual(Hex("05 04 20 29"), AkSignalFrame.Build(5, AkSignalFrame.GetLight));
        CollectionAssert.AreEqual(Hex("05 04 22 2B"), AkSignalFrame.Build(5, AkSignalFrame.SetLight));
        CollectionAssert.AreEqual(Hex("05 04 23 2C"), AkSignalFrame.Build(5, AkSignalFrame.SetManual));
        CollectionAssert.AreEqual(Hex("00 05 20 09 2E"), AkSignalFrame.Answer(AkSignalFrame.GetLight, 0x09));
    }

    [TestMethod]
    public void Svetlo_AutomatikaATma()
    {
        var sim = Create(TwilightMode.Dark);
        var server = new IniSrvAkSignal(sim, Line, Address, false);

        // pokoj: vypnute, svetla v poriadku, tma
        Assert.AreEqual(IniSrvAkSignal.ModeOff | IniSrvAkSignal.Twilight, server.CheckLight());
        Assert.AreEqual(1, server.Sent);

        // vlak potrebuje svetlo - server zapne automatiku a svetlo v tme svieti
        server.RequireAuto = true;
        Assert.AreEqual(IniSrvAkSignal.ModeAuto | IniSrvAkSignal.LightOn | IniSrvAkSignal.Twilight, server.CheckLight());
        Assert.AreEqual(2, server.Sent, "dotaz a zapnutie automatiky");
        Assert.IsTrue(Display(sim).IsLightOn);
        CollectionAssert.AreEqual(new[] { "LIGHT AUTO DARK ON" }, BoardText.Rows(sim.Read(s => s.FindBoard("6-5"))!).ToArray());

        // dalsie kolo uz automatiku nemeni
        server.CheckLight();
        Assert.AreEqual(1, server.Sent);

        // vlak odisiel - server automatiku vypne
        server.RequireAuto = false;
        Assert.AreEqual(IniSrvAkSignal.ModeOff | IniSrvAkSignal.Twilight, server.CheckLight());
        Assert.IsFalse(Display(sim).AutoMode);
    }

    [TestMethod]
    public void Svetlo_CezDenAutomatikaNesvieti()
    {
        var sim = Create(TwilightMode.Light);
        var server = new IniSrvAkSignal(sim, Line, Address, false) { RequireAuto = true };

        Assert.AreEqual(IniSrvAkSignal.ModeAuto, server.CheckLight());
        Assert.IsFalse(Display(sim).IsLightOn);
    }

    [TestMethod]
    public void Svetlo_RucneMaPrednost()
    {
        var sim = Create(TwilightMode.Light);
        var server = new IniSrvAkSignal(sim, Line, Address, false) { RequireAuto = true };
        server.CheckLight();

        // rucne svietenie aj s automatikou je rezim rucne a svieti aj cez den
        Assert.AreEqual(IniSrvAkSignal.ModeForce | IniSrvAkSignal.LightOn, server.CheckLight(IniSrvAkSignal.ModeForce));
        Assert.IsTrue(Display(sim).Manual);

        // zrusenie vrati automatiku
        Assert.AreEqual(IniSrvAkSignal.ModeAuto, server.CheckLight(IniSrvAkSignal.ModeAuto));
        Assert.IsFalse(Display(sim).Manual);
    }

    [TestMethod]
    public void Svetlo_PoruchaSvetiel()
    {
        var sim = Create(TwilightMode.Dark, new AkSignalConditions { LightsFault = true });
        var server = new IniSrvAkSignal(sim, Line, Address, false);

        Assert.AreEqual(IniSrvAkSignal.Twilight | IniSrvAkSignal.Failure, server.CheckLight());
        CollectionAssert.AreEqual(Hex("00 05 20 04 29"), Replies(sim, AkSignalFrame.Build(Address, AkSignalFrame.GetLight)));
    }

    [TestMethod]
    public void Svetlo_SnimacPodlaSlnka()
    {
        // 1. 10. 2026 12:00 UTC je na Slovensku den, 22:00 UTC noc
        var time = new ManualTime();
        var sim = Create(TwilightMode.Sun, time: time);
        var server = new IniSrvAkSignal(sim, Line, Address, false);

        Assert.AreEqual(0, server.CheckLight() & IniSrvAkSignal.Twilight);
        time.Advance(TimeSpan.FromHours(10));
        Assert.AreNotEqual(0, server.CheckLight() & IniSrvAkSignal.Twilight);
    }

    [TestMethod]
    public void Tik_TmaBezDotazuServera()
    {
        var time = new ManualTime();
        var sim = Create(TwilightMode.Sun, time: time);
        var server = new IniSrvAkSignal(sim, Line, Address, false) { RequireAuto = true };
        server.CheckLight();
        Assert.IsFalse(Display(sim).IsLightOn);

        time.Advance(TimeSpan.FromHours(10));
        Assert.AreEqual(1, sim.Tick(), "zmena tmy a ticho servera v jednom tiku");
        Assert.IsTrue(Display(sim).Dark);
        Assert.IsTrue(Display(sim).IsLightOn);
        Assert.AreEqual(time.GetUtcNow(), Display(sim).LightChangedAt);
        Assert.IsTrue(Display(sim).ServerSilent, "server sa 10 hodin nepytal");
        Assert.AreEqual(0, sim.Tick());

        server.CheckLight();
        Assert.IsFalse(Display(sim).ServerSilent);
    }

    [TestMethod]
    public void Rozhlas_ZapnutieAVypnutie()
    {
        var sim = Create();
        var server = new IniSrvAkSignal(sim, Line, Address, false);

        // v kludne: napajanie zosilnovaca v poriadku, inak server hlasi chybu napajania
        Assert.AreEqual(AkSignalDisplay.AudioAmpPower, server.Audio());

        Assert.AreEqual(AkSignalDisplay.AudioAmpPower | AkSignalDisplay.AudioPlaying, server.SendLight(AkSignalFrame.SetAudio, 1));
        Assert.AreEqual(1, Display(sim).Playing(0)?.Value);
        CollectionAssert.AreEqual(new[] { "LIGHT OFF", "AUDIO ON" }, BoardText.Rows(sim.Read(s => s.FindBoard("6-5"))!).ToArray());

        Assert.AreEqual(AkSignalDisplay.AudioAmpPower, server.SendLight(AkSignalFrame.ClearAudio));
        var announcement = Display(sim).Announcements.Single();
        Assert.AreEqual(AnnouncementResult.Completed, announcement.Result);
        Assert.AreEqual(1, announcement.Value);
        Assert.AreEqual(0, announcement.Channel);
    }

    [TestMethod]
    public void Rozhlas_VyssiaPrioritaZrusiHlasenie()
    {
        var sim = Create();
        var server = new IniSrvAkSignal(sim, Line, Address, false);
        server.SendLight(AkSignalFrame.SetAudio, 1);

        SetConditions(sim, new AkSignalConditions { HighPriority = true });
        // server pocas hrania kontroluje bit 0x04 a pri nom hlasenie ukonci s chybou
        var state = server.Audio()!.Value;
        Assert.AreEqual(AkSignalDisplay.AudioHighPriority | AkSignalDisplay.AudioCancelled | AkSignalDisplay.AudioAmpPower, state);
        Assert.AreEqual(AnnouncementResult.Cancelled, Display(sim).Announcements.Single().Result);

        // vypnutie rozhlasu zrusenie zmaze
        Assert.AreEqual(AkSignalDisplay.AudioHighPriority | AkSignalDisplay.AudioAmpPower, server.SendLight(AkSignalFrame.ClearAudio));
    }

    [TestMethod]
    public void Rozhlas_PoruchyNapajaniaAVystupu()
    {
        var sim = Create(conditions: new AkSignalConditions { AmpSupplyMissing = true, OutputPowerError = true });
        var server = new IniSrvAkSignal(sim, Line, Address, false);

        Assert.AreEqual(AkSignalDisplay.AudioOutputError, server.Audio());
    }

    [TestMethod]
    public void Chyby_SucetNeznamyPrikazSum()
    {
        var sim = Create();

        // zly sucet: ustredna prikaz nevykona a neodpovie
        Assert.IsEmpty(Replies(sim, Hex("05 04 22 2C")));
        Assert.IsFalse(Display(sim).AutoMode);

        // neznamy prikaz: bez odpovede
        Assert.IsEmpty(Replies(sim, AkSignalFrame.Build(Address, 0x30)));

        // sum pred prikazom sa zahodi
        CollectionAssert.AreEqual(Hex("00 05 22 09 30"), Replies(sim, [0xAA, .. AkSignalFrame.Build(Address, AkSignalFrame.SetLight)]));
        Assert.IsTrue(Display(sim).AutoMode);

        // na inej adrese nikto nie je
        Assert.IsEmpty(Replies(sim, AkSignalFrame.Build(7, AkSignalFrame.GetLight)));
        Assert.AreEqual(4L, sim.Read(s => s.FindLine(Line)!.Stats.Frames));
    }

    [TestMethod]
    public void Poruchy_OdpovedServerOdmietne()
    {
        var sim = Create();
        var server = new IniSrvAkSignal(sim, Line, Address, false);

        sim.SetFault("6-5", new BoardFault(FaultKind.Nak, 0, 0));
        Assert.AreEqual(IniSrvAkSignal.NotConnected, server.CheckLight());
        Assert.AreEqual(5, server.Sent, "server prikaz zopakuje patkrat");

        sim.SetFault("6-5", new BoardFault(FaultKind.Garbage, 0, 0));
        Assert.IsNull(server.Audio());

        sim.SetFault("6-5", new BoardFault(FaultKind.NoReply, 0, 0));
        Assert.IsNull(server.Audio());

        // zvoleny stav: rucne svietenie bez poruchy
        sim.SetFault("6-5", new BoardFault(FaultKind.Status, 0, 0x0A));
        Assert.AreEqual(IniSrvAkSignal.ModeForce | IniSrvAkSignal.LightOn, server.CheckLight());
    }

    [TestMethod]
    public void Napajanie_VypadokVymazeStav()
    {
        var sim = Create(TwilightMode.Dark);
        var server = new IniSrvAkSignal(sim, Line, Address, false) { RequireAuto = true };
        server.CheckLight();

        sim.SetPower("6-5", false);
        Assert.AreEqual(IniSrvAkSignal.NotConnected, server.CheckLight());
        sim.SetPower("6-5", true);
        Assert.IsFalse(Display(sim).AutoMode);

        // po navrate server automatiku zapne znova
        Assert.AreEqual(IniSrvAkSignal.ModeAuto | IniSrvAkSignal.LightOn | IniSrvAkSignal.Twilight, server.CheckLight());
    }
}
