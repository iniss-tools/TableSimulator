namespace TableSimulator.Contracts.Settings;

/// <summary>Zmena nastavenia servera (PUT /api/settings).</summary>
public sealed record UpdateSettingsRequest
{
    public int HttpPort { get; init; }
    public bool AllowNetwork { get; init; }
    public string FontsFolder { get; init; } = "";
    public string DesignsFolder { get; init; } = "";
    public string ConnectHost { get; init; } = "";
    public string MonitorFolder { get; init; } = "";
    public bool RequireLogin { get; init; }
    public IReadOnlyList<string>? VirtualFolders { get; init; }

    /// <summary>Priecinok aplikacie INISSView so sablonami XSL; prazdny = bez sablon.</summary>
    public string InissViewFolder { get; init; } = "";

    /// <summary>Oznamenia o vypadkoch liniek; null = nechat ulozene.</summary>
    public AlertSettingsRequest? Alerts { get; init; }
    public string InissProgram { get; init; } = "";
    public string InissRegistry { get; init; } = "";
    public bool InissElevated { get; init; }

    /// <summary>Port HTTPS; 0 = bez HTTPS.</summary>
    public int HttpsPort { get; init; }

    public bool HttpsRedirect { get; init; } = true;
    public string HttpsCertificate { get; init; } = "";
    public string HttpsKey { get; init; } = "";

    /// <summary>Heslo certifikatu alebo kluca; null = nechat ulozene, prazdne = zmazat.</summary>
    public string? HttpsPassword { get; init; }

    /// <summary>Zistovat novu verziu servera.</summary>
    public bool CheckUpdates { get; init; } = true;
}
