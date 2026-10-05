namespace TableSimulator.Application.Settings;

/// <summary>Ulozene nastavenie servera - implementuje server (subor JSON).</summary>
public interface ISettingsStore
{
    /// <summary>Aktualne nastavenie.</summary>
    AppSettings Current { get; }

    /// <summary>Ulozi a pouzije nove nastavenie.</summary>
    void Save(AppSettings settings);
}
