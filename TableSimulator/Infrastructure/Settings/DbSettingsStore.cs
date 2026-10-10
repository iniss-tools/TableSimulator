using TableSimulator.Application.Settings;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Settings;

namespace TableSimulator.Infrastructure.Settings;

/// <summary>
/// Nastavenie servera v databaze. Nacita sa pri starte (<see cref="Persistence.DatabaseBootstrap" />) a drzi v pamati,
/// zmena sa hned zapise. Port z prikazoveho riadka (<c>--port</c>) prebije ulozeny.
/// </summary>
public sealed class DbSettingsStore : ISettingsStore
{
    private readonly DatabaseConnection _database;
    private readonly Lock _lock = new();
    private AppSettings _current;

    public DbSettingsStore(DatabaseConnection database, AppSettings loaded, int? portOverride)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(loaded);
        _database = database;
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
        using (var db = ServerDatabase.Open(_database))
            new SettingsRepository(db).Save(settings);
        lock (_lock)
            _current = settings;
    }
}
