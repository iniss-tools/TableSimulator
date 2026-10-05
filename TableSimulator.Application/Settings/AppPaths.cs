namespace TableSimulator.Application.Settings;

/// <summary>
/// Priecinky a subory servera: nastavenia (predvolene v profile pouzivatela, parametrom <c>--data</c> inde), linky
/// a tabule (<c>--config</c>), zaznamy, logy a webove rozhranie.
/// </summary>
/// <param name="DataFolder">priecinok nastaveni</param>
/// <param name="ConfigPath">subor s linkami a tabulami</param>
/// <param name="WebRoot">priecinok weboveho rozhrania (<c>wwwroot</c>)</param>
public sealed record AppPaths(string DataFolder, string ConfigPath, string WebRoot)
{
    /// <summary>Predvoleny priecinok nastaveni v profile pouzivatela.</summary>
    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TableSimulator");

    /// <summary>Nastavenie servera.</summary>
    public string SettingsFile => Path.Combine(DataFolder, "settings.json");

    /// <summary>Zaznamy komunikacie.</summary>
    public string RecordingsFolder => Path.Combine(DataFolder, "recordings");

    /// <summary>Databaza pouzivatelov (SQLite).</summary>
    public string DatabaseFile => Path.Combine(DataFolder, "tablesimulator.db");

    /// <summary>Kluce ochrany dat (podpis prihlasovacich cookie) - s priecinkom nastaveni sa prenasaju.</summary>
    public string KeysFolder => Path.Combine(DataFolder, "keys");

    /// <summary>Logy servera.</summary>
    public string LogsFolder => Path.Combine(DataFolder, "logs");

    /// <summary>Vstavane vzhlady.</summary>
    public string BuiltInDesignsFolder => Path.Combine(WebRoot, "designs");

    /// <summary>Priecinok vlastnych vzhladov podla nastavenia.</summary>
    public string DesignsFolder(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return string.IsNullOrWhiteSpace(settings.DesignsFolder) ? Path.Combine(DataFolder, "designs") : settings.DesignsFolder;
    }
}
