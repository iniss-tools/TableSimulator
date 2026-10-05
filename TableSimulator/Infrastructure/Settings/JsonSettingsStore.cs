using System.Text.Json;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Settings;

/// <summary>
/// Nastavenie servera v subore <c>settings.json</c> priecinka nastaveni. Port z prikazoveho riadka (<c>--port</c>)
/// prebije ulozeny.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly AppPaths _paths;
    private readonly Lock _lock = new();
    private AppSettings _current;

    public JsonSettingsStore(AppPaths paths, int? portOverride)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths = paths;
        var loaded = Load(paths.SettingsFile);
        _current = portOverride is { } port ? loaded with { HttpPort = port } : loaded;
    }

    /// <inheritdoc />
    public AppSettings Current
    {
        get
        {
            lock (_lock)
                return _current;
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(_paths.DataFolder);
        File.WriteAllText(_paths.SettingsFile, JsonSerializer.Serialize(settings, JsonDefaults.Files));
        lock (_lock)
            _current = settings;
    }

    /// <summary>Chybajuci alebo poskodeny subor = predvolene nastavenie.</summary>
    private static AppSettings Load(string file)
    {
        try
        {
            return File.Exists(file) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file), JsonDefaults.Files) ?? new AppSettings() : new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }
}
