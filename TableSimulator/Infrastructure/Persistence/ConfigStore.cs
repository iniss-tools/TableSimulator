using System.Text.Json;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Settings;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Infrastructure.Persistence;

/// <summary>
/// Linky a tabule simulatora v subore JSON - po kazdej zmene sa ulozia s kratkym odkladom.
/// </summary>
public sealed class ConfigStore : IDisposable
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly Simulator _simulator;
    private readonly ILogger<ConfigStore> _logger;
    private readonly Timer _timer;

    public ConfigStore(Simulator simulator, AppPaths paths, ILogger<ConfigStore> logger)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _simulator = simulator;
        _logger = logger;
        Path = paths.ConfigPath;
        _timer = new Timer(_ => Save(), null, Timeout.Infinite, Timeout.Infinite);
        _simulator.Changed += OnChanged;
    }

    /// <summary>Subor liniek a tabul.</summary>
    public string Path { get; }

    /// <summary>Nacita linky a tabule; ak subor chyba alebo je poskodeny, zacne jednou linkou ELEN so samoucenim.</summary>
    public void Load()
    {
        SimulatorConfig? config = null;
        if (File.Exists(Path))
        {
            try
            {
                config = JsonSerializer.Deserialize<SimulatorConfig>(File.ReadAllText(Path), JsonDefaults.Files);
            }
            catch (JsonException e)
            {
                var backup = Path + ".bad";
                File.Copy(Path, backup, true);
                ServerLog.ConfigBroken(_logger, e, Path, backup);
            }
        }

        if (config is null)
        {
            ServerLog.ConfigCreated(_logger, Path);
            config = new SimulatorConfig { Lines = [new LineConfig(1, 4, LineEndpoint.DefaultFor(1).ToString())] };
        }
        else
        {
            ServerLog.ConfigLoaded(_logger, Path, config.Lines.Count, config.Boards.Count);
        }

        _simulator.Load(config);
    }

    /// <summary>Naplanuje ulozenie (viac zmien za sebou sa ulozi raz).</summary>
    public void ScheduleSave() => _timer.Change(SaveDelay, Timeout.InfiniteTimeSpan);

    /// <summary>Ulozi hned; chybu zapise do zaznamu (casovac nesmie spadnut).</summary>
    public void Save()
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            var temp = Path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_simulator.ToConfig(), JsonDefaults.Files));
            File.Move(temp, Path, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ServerLog.ConfigSaveFailed(_logger, e, Path);
        }
    }

    private void OnChanged(SimulatorEvent e)
    {
        if (e is StructureChanged or BoardChanged)
            ScheduleSave();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _simulator.Changed -= OnChanged;
        _timer.Dispose();
    }
}
