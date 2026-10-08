namespace TableSimulator.Persistence.Settings;

/// <summary>Nastavenie servera v databaze - jediny riadok tabulky <c>ServerSettings</c>.</summary>
public sealed class SettingsRecord
{
    /// <summary>Jediny riadok.</summary>
    public const int SingleId = 1;

    public int Id { get; set; } = SingleId;

    public int HttpPort { get; set; }

    public bool AllowNetwork { get; set; }

    public bool RequireLogin { get; set; }

    public string FontsFolder { get; set; } = "";

    public string DesignsFolder { get; set; } = "";

    public string ConnectHost { get; set; } = "";

    public string MonitorFolder { get; set; } = "";

    /// <summary>Priecinky logov INISSu (v databaze pole JSON).</summary>
    public List<string> VirtualFolders { get; set; } = [];

    public string InissProgram { get; set; } = "";

    public string InissRegistry { get; set; } = "";

    public bool InissElevated { get; set; }

    public int HttpsPort { get; set; }

    public bool HttpsRedirect { get; set; }

    public string HttpsCertificate { get; set; } = "";

    public string HttpsKey { get; set; } = "";

    /// <summary>Heslo certifikatu zasifrovane ochranou dat servera.</summary>
    public string HttpsPassword { get; set; } = "";
}
