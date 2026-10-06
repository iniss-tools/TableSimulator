namespace TableSimulator.Application.Settings;

/// <summary>
/// Priecinky a subory servera: priecinok nastaveni (predvolene v profile pouzivatela, parametrom <c>--data</c> inde)
/// s databazou, zaznamami a logmi a webove rozhranie.
/// </summary>
/// <param name="DataFolder">priecinok nastaveni</param>
/// <param name="WebRoot">priecinok weboveho rozhrania (<c>wwwroot</c>)</param>
public sealed record AppPaths(string DataFolder, string WebRoot)
{
    /// <summary>Predvoleny priecinok nastaveni v profile pouzivatela.</summary>
    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TableSimulator");

    /// <summary>Stary subor nastavenia servera - pri prvom starte s databazou sa do nej prevedie.</summary>
    public string LegacySettingsFile => Path.Combine(DataFolder, "settings.json");

    /// <summary>Stary subor liniek a tabul - pri prvom starte s databazou sa do nej prevedie.</summary>
    public string LegacyConfigFile => Path.Combine(DataFolder, "config.json");

    /// <summary>Zaznamy komunikacie.</summary>
    public string RecordingsFolder => Path.Combine(DataFolder, "recordings");

    /// <summary>Databaza servera (SQLite) - nastavenie, linky a tabule, pouzivatelia, audit, virtualne tabule.</summary>
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
