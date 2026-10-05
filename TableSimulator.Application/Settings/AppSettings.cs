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

    /// <summary>Webove rozhranie dostupne aj z inych zariadeni v sieti (inak len z tohto pocitaca).</summary>
    public bool AllowNetwork { get; init; }

    /// <summary>Priecinok s pismami ELEN (.bin); prazdny = bez pisiem (priblizne pismo prehliadaca).</summary>
    public string FontsFolder { get; init; } = "";

    /// <summary>Priecinok vlastnych vzhladov; prazdny = <c>designs</c> v priecinku nastaveni.</summary>
    public string DesignsFolder { get; init; } = "";

    /// <summary>Adresa tohto pocitaca z pohladu INISSu (pre <c>TablePort</c>); prazdna = 127.0.0.1.</summary>
    public string ConnectHost { get; init; } = "";

    /// <summary>Priecinok programu ELEN Monitor - predlohy tabul, pisma a projekty; prazdny = bez Monitora.</summary>
    public string MonitorFolder { get; init; } = "";
}
