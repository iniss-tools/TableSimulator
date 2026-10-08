using System.Text.Json.Serialization;

namespace TableSimulator.Application.Settings;

/// <summary>
/// Nastavenie servera simulatora (nie liniek a tabul - tie su v subore konfiguracie simulatora).
/// </summary>
public sealed record AppSettings
{
    /// <summary>Predvoleny port weboveho rozhrania.</summary>
    public const int DefaultHttpPort = 5470;

    /// <summary>Port weboveho rozhrania.</summary>
    public int HttpPort { get; init; } = DefaultHttpPort;

    /// <summary>Webove rozhranie dostupne aj z inych zariadeni v sieti (inak len z tohto pocitaca) - vtedy vzdy s prihlasenim.</summary>
    public bool AllowNetwork { get; init; }

    /// <summary>Vyzadovat prihlasenie aj z tohto pocitaca (pri pristupe zo siete sa vyzaduje vzdy).</summary>
    public bool RequireLogin { get; init; }

    /// <summary>Server pouzije prihlasovanie - nastavene alebo vynutene pristupom zo siete.</summary>
    [JsonIgnore]
    public bool LoginRequired => RequireLogin || AllowNetwork;

    /// <summary>Priecinok s pismami ELEN (.bin); prazdny = bez pisiem (priblizne pismo prehliadaca).</summary>
    public string FontsFolder { get; init; } = "";

    /// <summary>Priecinok vlastnych vzhladov; prazdny = <c>designs</c> v priecinku nastaveni.</summary>
    public string DesignsFolder { get; init; } = "";

    /// <summary>Adresa tohto pocitaca z pohladu INISSu (pre <c>TablePort</c>); prazdna = 127.0.0.1.</summary>
    public string ConnectHost { get; init; } = "";

    /// <summary>Priecinok programu ELEN Monitor - predlohy tabul, pisma a projekty; prazdny = bez Monitora.</summary>
    public string MonitorFolder { get; init; } = "";

    /// <summary>
    /// Priecinky logov INISSu (<c>PathNames\LogPath</c>) s XML tabul pre virtualne tabule; rovnomenny subor sa berie
    /// novsi, pri rovnakom case z prveho priecinka.
    /// </summary>
    public IReadOnlyList<string> VirtualFolders { get; init; } = [];

    /// <summary>Program INISSu (exe) na tomto pocitaci - odkial simulator nacita linky a tabule stanice; prazdny = nenastaveny.</summary>
    public string InissProgram { get; init; } = "";

    /// <summary>Vetva registra INISSu (hodnota /Reg:); prazdna = meno programu bez pripony.</summary>
    public string InissRegistry { get; init; } = "";

    /// <summary>INISS sa spusta ako spravca - cita HKLM priamo, nie kopiu vo VirtualStore.</summary>
    public bool InissElevated { get; init; }

    /// <summary>Port HTTPS; 0 = bez HTTPS. Zmena sa prejavi po restarte.</summary>
    public int HttpsPort { get; init; }

    /// <summary>Pristup zo siete cez HTTP presmerovat na HTTPS (z tohto pocitaca ostava HTTP).</summary>
    public bool HttpsRedirect { get; init; } = true;

    /// <summary>Vlastny certifikat (.pfx, .p12 alebo PEM); prazdny = certifikat od certifikacnej autority simulatora.</summary>
    public string HttpsCertificate { get; init; } = "";

    /// <summary>Subor kluca PEM k vlastnemu certifikatu; prazdny = kluc je v subore certifikatu.</summary>
    public string HttpsKey { get; init; } = "";

    /// <summary>Heslo vlastneho certifikatu alebo kluca zasifrovane ochranou dat servera (<see cref="ISecretProtector" />).</summary>
    public string HttpsPassword { get; init; } = "";
}
