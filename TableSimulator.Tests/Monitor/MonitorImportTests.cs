using TableSimulator.Application.Monitor;
using TableSimulator.Application.Settings;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Tests.Monitor;

/// <summary>
/// Import projektu ELEN Monitora: typy panelov su tabule ELEN, preto ide len na linku s tabulami ELEN.
/// </summary>
[TestClass]
public class MonitorImportTests
{
    private sealed class TestSettings : ISettingsStore
    {
        public AppSettings Current { get; private set; } = new();

        public void Save(AppSettings settings) => Current = settings;
    }

    [TestMethod]
    [DataRow(5, "LCD1")]
    [DataRow(1, "LCD")]
    [DataRow(128, "LCD1")]
    public async Task Import_LinkaBezTabulElenOdmietnuta(int tableClass, string manufacturer)
    {
        var simulator = new Simulator();
        simulator.SetLine(new LineConfig(2, tableClass, "TCP://127.0.0.1:47002") { Manufacturer = manufacturer });
        var handler = new ImportMonitorProjectCommandHandler(simulator, new TestSettings());

        var result = await handler.Handle(new ImportMonitorProjectCommand("projekt", 2), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("monitor.notElenLine", result.Error.Code);
        Assert.IsEmpty(simulator.Read(s => s.Boards.ToList()));
    }

    [TestMethod]
    public async Task Import_LinkaElenPokracujeNaProjekt()
    {
        var simulator = new Simulator();
        simulator.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001"));
        var handler = new ImportMonitorProjectCommandHandler(simulator, new TestSettings());

        var result = await handler.Handle(new ImportMonitorProjectCommand("projekt", 1), CancellationToken.None);

        Assert.AreEqual("monitor.projectNotFound", result.Error.Code);
    }
}
