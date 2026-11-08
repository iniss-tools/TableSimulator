using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Scenarios;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.AkSignal;
using TableSimulator.Core.Scenarios;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;
using TableSimulator.Tests.Protocols.AkSignal;

namespace TableSimulator.Tests.Scenarios;

/// <summary>
/// Krok scenara pre ustrednu AK Signal: stavy a snimac zo scenara sa pridaju k nastaveniu ustredne (server ich vidi
/// v odpovedi), plati len pocas kroku a nastavenie ustredne sa nemeni; ine tabule krok obide.
/// </summary>
[TestClass]
public sealed class AkSignalScenarioTests
{
    private const int Line = 6, Address = 5;

    private sealed class NoLive : ILiveNotifier
    {
        public void Publish(LiveMessage message)
        {
        }
    }

    private static Simulator Station(ManualTime time)
    {
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(Line, 131, "TCP://127.0.0.1:47006") { AutoLearn = false, Manufacturer = AkSignalProtocol.Manufacturer });
        sim.SetBoard(new BoardConfig(Line, Address, AkSignalProtocol.Manufacturer)
        {
            Profile = new BoardProfile
            {
                AkSignal = new AkSignalProfile
                {
                    Sensor = new TwilightSensor { Mode = TwilightMode.Light }, Conditions = new AkSignalConditions { OutputPowerError = true }
                }
            }
        });
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001") { AutoLearn = false });
        sim.SetBoard(new BoardConfig(1, 5, "ELEN"));
        return sim;
    }

    private static ScenarioStep Device(int at, int duration, int line, int? address, AkSignalConditions conditions, TwilightMode? sensor) =>
        new(at, duration, line, address, ScenarioAction.Device) { Device = new AkSignalOverride(conditions, sensor) };

    [TestMethod]
    public void Ucinky_StavyZoVsetkychKrokovSnimacZNeskorsieho()
    {
        var scenario = new FaultScenario("Test", false, null,
        [
            Device(0, 1000, Line, Address, new AkSignalConditions { LightsFault = true }, TwilightMode.Dark),
            Device(0, 1000, Line, null, new AkSignalConditions { HighPriority = true }, TwilightMode.Sun),
            Device(0, 1000, Line, Address, AkSignalConditions.None, null)
        ]);

        var effects = ScenarioTimeline.Effects(scenario, [0, 1, 2], line => line == Line ? ["6-5", "6-7"] : []);

        var device = effects.Devices["6-5"];
        Assert.AreEqual(new AkSignalConditions { LightsFault = true, HighPriority = true }, device.Conditions);
        Assert.AreEqual(TwilightMode.Sun, device.Sensor, "posledny krok snimac neurcuje - plati predchadzajuci");
        Assert.AreEqual(new AkSignalConditions { HighPriority = true }, effects.Devices["6-7"].Conditions);
        Assert.IsEmpty(effects.Faults);
    }

    [TestMethod]
    public async Task Spustac_ServerVidiStavZoScenaraLenPocasKroku()
    {
        var time = new ManualTime();
        var sim = Station(time);
        await using var transport = new TransportManager(sim);
        using var runner = new ScenarioRunner(sim, transport, new NoLive(), time);
        var server = new IniSrvAkSignal(sim, Line, Address, false);
        sim.Tick();
        Assert.AreEqual(IniSrvAkSignal.ModeOff, server.CheckLight());

        // porucha svetiel a tma len zo scenara, krok pre vsetky tabule linky aj linku s tabulou ELEN
        await runner.StartAsync(1, new FaultScenario("Test", false, null,
        [
            Device(1000, 2000, Line, null, new AkSignalConditions { LightsFault = true }, TwilightMode.Dark),
            Device(1000, 2000, 1, null, new AkSignalConditions { LightsFault = true }, null)
        ]));
        time.Advance(TimeSpan.FromMilliseconds(1000));
        await runner.TickAsync();

        Assert.AreEqual(IniSrvAkSignal.ModeOff | IniSrvAkSignal.Twilight | IniSrvAkSignal.Failure, server.CheckLight());
        var board = sim.Read(s => s.FindBoard("6-5"))!;
        Assert.AreEqual(new AkSignalConditions { OutputPowerError = true }, board.Profile.AkSignal!.Conditions, "nastavenie ustredne sa nemeni");
        Assert.AreEqual(TwilightMode.Light, board.Profile.AkSignal.Sensor.Mode);
        var dto = DtoMapper.ToDto(board).Display.AkSignal!;
        Assert.IsTrue(dto.Conditions.LightsFault && dto.Conditions.OutputPowerError, "stav, podla ktoreho ustredna odpoveda");
        Assert.IsNotNull(dto.Scenario);
        Assert.IsNull(sim.Read(s => s.FindBoard("1-5"))!.Override, "tabula ELEN prekrytie nema");

        // po kroku plati zase nastavenie ustredne
        time.Advance(TimeSpan.FromMilliseconds(2500));
        await runner.TickAsync();
        Assert.AreEqual(IniSrvAkSignal.ModeOff, server.CheckLight());
        Assert.IsNull(DtoMapper.ToDto(sim.Read(s => s.FindBoard("6-5"))!).Display.AkSignal!.Scenario);
        Assert.IsNull(runner.Status(1));
    }

    [TestMethod]
    public void Mapovanie_KrokUstredneZApiDoJadra()
    {
        var request = new Contracts.Scenarios.FaultScenarioRequest("Test", false, null,
        [
            new Contracts.Scenarios.ScenarioStepDto(0, 1000, Line, Address, Contracts.Scenarios.ScenarioAction.Device, null)
            {
                Device = new Contracts.Scenarios.ScenarioDeviceDto(new Contracts.Boards.AkSignalConditionsDto(BankNotSynced: true), Contracts.Boards.TwilightMode.Dark)
            }
        ]);

        var step = ScenarioMapping.ToCore(ScenarioMapping.Deserialize(ScenarioMapping.Serialize(request))).Steps.Single();

        Assert.AreEqual(ScenarioAction.Device, step.Action);
        Assert.AreEqual(new AkSignalOverride(new AkSignalConditions { BankNotSynced = true }, TwilightMode.Dark), step.Device);
    }

    [TestMethod]
    public void Overenie_KrokUstredneMusiNieconastavit()
    {
        var validator = new FaultScenarioRequestValidator();
        Contracts.Scenarios.FaultScenarioRequest With(Contracts.Scenarios.ScenarioDeviceDto? device) => new("Test", false, null,
            [new Contracts.Scenarios.ScenarioStepDto(0, 1000, Line, Address, Contracts.Scenarios.ScenarioAction.Device, null) { Device = device }]);

        Assert.IsFalse(validator.Validate(With(null)).IsValid);
        Assert.IsFalse(validator.Validate(With(new Contracts.Scenarios.ScenarioDeviceDto(new Contracts.Boards.AkSignalConditionsDto(), null))).IsValid);
        Assert.IsTrue(validator.Validate(With(new Contracts.Scenarios.ScenarioDeviceDto(new Contracts.Boards.AkSignalConditionsDto(), Contracts.Boards.TwilightMode.Light))).IsValid);
        Assert.IsTrue(validator.Validate(With(new Contracts.Scenarios.ScenarioDeviceDto(new Contracts.Boards.AkSignalConditionsDto(HighPriority: true), null))).IsValid);
    }
}
