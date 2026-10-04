using System.Globalization;
using System.Text.Json.Serialization;
using TableSimulator.Core.Protocols;

namespace TableSimulator.Core.Lines;

/// <summary>
/// Sietovy prenos linky.
/// </summary>
public enum LineTransport
{
    [JsonStringEnumMemberName("tcp")] Tcp,
    [JsonStringEnumMemberName("udp")] Udp
}

/// <summary>
/// Kde linka simulatora pocuva: <c>TCP://host:port</c> alebo <c>UDP://host:port</c> (rovnaky tvar ako <c>TablePort</c> v INISSe).
/// </summary>
/// <param name="Transport">TCP alebo UDP</param>
/// <param name="Host">adresa rozhrania; <c>0.0.0.0</c> = vsetky</param>
/// <param name="Port">cislo portu</param>
public sealed record LineEndpoint(LineTransport Transport, string Host, int Port)
{
    /// <summary>Prvy port pri automatickom pridelovani: linka N pocuva na porte <c>BasePort + N</c>.</summary>
    public const int BasePort = 47000;

    /// <summary>Predvoleny koncovy bod linky <paramref name="line" /> (TCP na vsetkych rozhraniach).</summary>
    public static LineEndpoint DefaultFor(int line) => new(LineTransport.Tcp, "0.0.0.0", BasePort + line);

    /// <summary>Rozlozi text <c>TCP://host:port</c>; null pri zlom tvare.</summary>
    public static LineEndpoint? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var s = text.Trim();
        LineTransport transport;
        if (s.StartsWith("TCP://", StringComparison.OrdinalIgnoreCase))
            transport = LineTransport.Tcp;
        else if (s.StartsWith("UDP://", StringComparison.OrdinalIgnoreCase))
            transport = LineTransport.Udp;
        else
            return null;

        var rest = s[6..];
        var colon = rest.LastIndexOf(':');
        if (colon <= 0 || !int.TryParse(rest[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 1 or > 65535)
            return null;

        return new LineEndpoint(transport, rest[..colon], port);
    }

    /// <summary>
    /// Hodnota <c>TablePort</c>, ktorou sa INISS na tuto linku pripoji: <c>N=TCP://host:port</c>.
    /// </summary>
    /// <param name="line">cislo linky v INISSe</param>
    /// <param name="connectHost">adresa tohto pocitaca z pohladu INISSu; null = adresa rozhrania, pri 0.0.0.0 127.0.0.1</param>
    public string ToInissTablePort(int line, string? connectHost = null)
    {
        var host = connectHost ?? (Host is "0.0.0.0" or "::" or "*" ? "127.0.0.1" : Host);
        return string.Create(CultureInfo.InvariantCulture, $"{line}={Scheme}://{host}:{Port}");
    }

    private string Scheme => Transport == LineTransport.Tcp ? "TCP" : "UDP";

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Scheme}://{Host}:{Port}");
}

/// <summary>
/// Rezim linky.
/// </summary>
public enum LineMode
{
    /// <summary>Tabule odpovedaju ako skutocne.</summary>
    [JsonStringEnumMemberName("display")] Display,

    /// <summary>Tabule len pocuvaju a neodpovedaju (skutocne tabule na linke odpovedaju samy).</summary>
    [JsonStringEnumMemberName("monitor")] Monitor
}

/// <summary>
/// Stav spojenia linky.
/// </summary>
public enum LineStatus
{
    [JsonStringEnumMemberName("stopped")] Stopped,
    [JsonStringEnumMemberName("listening")] Listening,
    [JsonStringEnumMemberName("connected")] Connected,
    [JsonStringEnumMemberName("error")] Error
}

/// <summary>
/// Pocitadla linky.
/// </summary>
public sealed class LineStats
{
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
    public long Frames { get; set; }
    public long BadFrames { get; set; }
    public long Discarded { get; set; }
    public long Connections { get; set; }
}

/// <summary>
/// Komunikacna linka simulatora - zodpoveda jednej sekcii <c>Driver*</c> v INISSe.
/// </summary>
public sealed class SimLine
{
    internal SimLine(int number, int tableClass, ILineProtocol protocol)
    {
        Number = number;
        TableClass = tableClass;
        Protocol = protocol;
    }

    /// <summary>Cislo linky 1-100 (ako v INISSe - podla neho sa k nej priraduju tabule).</summary>
    public int Number { get; }

    /// <summary>Trieda linky (<c>TableClass</c>) - rodina protokolu.</summary>
    public int TableClass { get; }

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; set; } = "";

    /// <summary>Kde linka pocuva.</summary>
    public LineEndpoint Endpoint { get; set; } = LineEndpoint.DefaultFor(1);

    /// <summary>Rezim - odpovedat alebo len pocuvat.</summary>
    public LineMode Mode { get; set; }

    /// <summary>Na spravu pre neznamu adresu vytvorit tabulu.</summary>
    public bool AutoLearn { get; set; } = true;

    /// <summary>Vyrobca tabul vytvorenych samoucenim.</summary>
    public string Manufacturer { get; set; } = "ELEN";

    /// <summary>Stav spojenia.</summary>
    public LineStatus Status { get; internal set; }

    /// <summary>Podrobnosti stavu - pripojeny klient alebo chyba.</summary>
    public string? StatusDetail { get; internal set; }

    /// <summary>Pocitadla (pri zmene nastavenia linky ostanu).</summary>
    public LineStats Stats { get; init; } = new();

    /// <summary>Zvysi sa pri kazdej zmene.</summary>
    public long Version { get; internal set; }

    internal ILineProtocol Protocol { get; }
}
