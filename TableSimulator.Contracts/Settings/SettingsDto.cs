namespace TableSimulator.Contracts.Settings;

/// <summary>Nastavenie servera pre webove rozhranie.</summary>
/// <param name="DatabasePath">databaza servera s nastavenim, linkami a tabulami (len na citanie)</param>
public sealed record SettingsDto(int HttpPort, bool AllowNetwork, string FontsFolder, string DesignsFolder, string ConnectHost, string DatabasePath)
{
    /// <summary>Priecinok programu ELEN Monitor.</summary>
    public string MonitorFolder { get; init; } = "";

    /// <summary>Vyzadovat prihlasenie aj z tohto pocitaca (pri pristupe zo siete sa vyzaduje vzdy).</summary>
    public bool RequireLogin { get; init; }

    /// <summary>Priecinky logov INISSu s XML tabul pre virtualne tabule (prvy ma prednost pri rovnakom case).</summary>
    public IReadOnlyList<string> VirtualFolders { get; init; } = [];

    /// <summary>Port HTTPS; 0 = bez HTTPS.</summary>
    public int HttpsPort { get; init; }

    /// <summary>Pristup zo siete cez HTTP presmerovat na HTTPS.</summary>
    public bool HttpsRedirect { get; init; } = true;

    /// <summary>Vlastny certifikat (.pfx, .p12 alebo PEM); prazdny = certifikat od certifikacnej autority simulatora.</summary>
    public string HttpsCertificate { get; init; } = "";

    /// <summary>Subor kluca PEM; prazdny = kluc je v subore certifikatu.</summary>
    public string HttpsKey { get; init; } = "";

    /// <summary>Heslo certifikatu je ulozene (samotne heslo server nevracia).</summary>
    public bool HttpsPasswordSet { get; init; }
}
