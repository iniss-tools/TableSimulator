using System.Text.Json;
using TableSimulator.Core.Simulation;

namespace TableSimulator;

/// <summary>
/// Ulozenie liniek a tabul simulatora do JSON - po kazdej zmene s kratkym odkladom.
/// </summary>
public sealed class ConfigStore : IDisposable
{
    private readonly Simulator _simulator;
    private readonly Timer _timer;

    public ConfigStore(Simulator simulator, string path)
    {
        _simulator = simulator;
        Path = path;
        _timer = new Timer(_ => Save(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Subor nastavenia liniek a tabul.</summary>
    public string Path { get; }

    /// <summary>Predvoleny subor v profile pouzivatela.</summary>
    public static string DefaultPath => System.IO.Path.Combine(AppSettings.DataFolder, "config.json");

    /// <summary>Nacita linky a tabule; ak subor chyba, vytvori jednu linku ELEN so samoucenim.</summary>
    public void Load()
    {
        SimulatorConfig? config = null;
        if (File.Exists(Path))
        {
            try
            {
                config = JsonSerializer.Deserialize<SimulatorConfig>(File.ReadAllText(Path), AppSettings.Json);
            }
            catch (JsonException)
            {
                File.Copy(Path, Path + ".bad", true);
            }
        }

        config ??= new SimulatorConfig
        {
            Lines = [new LineConfig(1, 4, Core.Lines.LineEndpoint.DefaultFor(1).ToString())]
        };

        _simulator.Load(config);
    }

    /// <summary>Naplanuje ulozenie (viac zmien za sebou sa ulozi raz).</summary>
    public void ScheduleSave() => _timer.Change(500, Timeout.Infinite);

    /// <summary>Ulozi hned.</summary>
    public void Save()
    {
        var directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var temp = Path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_simulator.ToConfig(), AppSettings.Json));
        File.Move(temp, Path, true);
    }

    /// <inheritdoc />
    public void Dispose() => _timer.Dispose();
}
