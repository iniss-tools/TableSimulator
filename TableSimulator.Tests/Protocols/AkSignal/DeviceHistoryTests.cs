using System.Text;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;
using TableSimulator.Core.Boards.Announcements;
using TableSimulator.Core.Boards.History;
using TableSimulator.Core.Protocols.AkSignal;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Tests.Protocols.AkSignal;

/// <summary>
/// Historia ustredni AK Signal v simulatore: stav po prvom tiku, zmeny od servera, poruchy z profilu, vypadok napajania
/// a skoncene hlasenia; odber vyprazdni zoznam.
/// </summary>
[TestClass]
public class DeviceHistoryTests
{
    private static Simulator Create(ManualTime time)
    {
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(6, 131, "TCP://127.0.0.1:47006") { AutoLearn = false, Manufacturer = AkSignalProtocol.Manufacturer });
        sim.SetBoard(new BoardConfig(6, 3, AkSignalProtocol.Manufacturer)
        {
            Profile = new BoardProfile { AkSignal = new AkSignalProfile { Sensor = new TwilightSensor { Mode = TwilightMode.Dark } } }
        });
        sim.SetLine(new LineConfig(7, 132, "TCP://127.0.0.1:47007") { AutoLearn = false, Manufacturer = AkSignalUhProtocol.Manufacturer });
        sim.SetBoard(new BoardConfig(7, 0, AkSignalUhProtocol.Manufacturer)
        {
            Profile = new BoardProfile { AkSignal = new AkSignalProfile { Sensor = new TwilightSensor { Mode = TwilightMode.Light }, SoundSeconds = 2 } }
        });
        return sim;
    }

    [TestMethod]
    public void Historia_ZmenyStavuAHlasenia()
    {
        var time = new ManualTime();
        var sim = Create(time);

        // pred prvym tikom snimac nie je vyhodnoteny - nic na zapis
        Assert.IsTrue(sim.TakeDeviceHistory().IsEmpty);
        sim.Tick();
        var initial = sim.TakeDeviceHistory().Lights;
        CollectionAssert.AreEquivalent(new[] { LightFlags.Dark, LightFlags.None }, initial.Select(l => l.Flags).ToArray());

        // server zapne automatiku - v tme svetlo svieti
        time.Advance(TimeSpan.FromSeconds(5));
        sim.Receive(6, AkSignalFrame.Build(3, AkSignalFrame.SetLight));
        var auto = sim.TakeDeviceHistory().Lights.Single();
        Assert.AreEqual((6, 3, LightFlags.Auto | LightFlags.Dark | LightFlags.On), (auto.Line, auto.Address, auto.Flags));
        Assert.AreEqual(time.GetUtcNow(), auto.At);

        // dotaz bez zmeny nic nezapise, porucha z profilu ano
        sim.Receive(6, AkSignalFrame.Build(3, AkSignalFrame.GetLight));
        Assert.IsTrue(sim.TakeDeviceHistory().IsEmpty);
        sim.UpdateProfile("6-3", p => p with { AkSignal = p.AkSignal! with { Conditions = new AkSignalConditions { LightsFault = true } } });
        Assert.AreEqual(LightFlags.Auto | LightFlags.Dark | LightFlags.On | LightFlags.Fault, sim.TakeDeviceHistory().Lights.Single().Flags);

        sim.SetPower("6-3", false);
        Assert.AreEqual(LightFlags.PoweredOff | LightFlags.Fault, sim.TakeDeviceHistory().Lights.Single().Flags);

        // hlasenie UH sa zapise az po skonceni
        sim.Receive(7, Encoding.UTF8.GetBytes("<aud|ch=1|CZ/a*CZ/b>"));
        Assert.IsEmpty(sim.TakeDeviceHistory().Announcements);
        time.Advance(TimeSpan.FromSeconds(4));
        sim.Tick();
        var done = sim.TakeDeviceHistory().Announcements.Single();
        Assert.AreEqual((7, 0, 1), (done.Line, done.Address, done.Announcement.Channel));
        Assert.AreEqual(AnnouncementResult.Completed, done.Announcement.Result);
        CollectionAssert.AreEqual(new[] { "CZ/a", "CZ/b" }, done.Announcement.Sounds.ToArray());

        // to iste hlasenie sa nezapise druhy raz
        sim.Receive(7, Encoding.UTF8.GetBytes("<aud>"));
        Assert.IsEmpty(sim.TakeDeviceHistory().Announcements);
    }
}
