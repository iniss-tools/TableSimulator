using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TableSimulator.Core.Protocols;

namespace TableSimulator.Core.Lines;

/// <summary>
/// Prenos linky.
/// </summary>
public enum LineTransport
{
    /// <summary>TCP server - INISS sa pripaja ako klient.</summary>
    [JsonStringEnumMemberName("tcp")] Tcp,

    /// <summary>UDP - INISS posiela datagramy.</summary>
    [JsonStringEnumMemberName("udp")] Udp,

    /// <summary>Pomenovana rura - server je INISS, simulator sa k nej pripaja ako klient.</summary>
    [JsonStringEnumMemberName("pipe")] Pipe,

    /// <summary>Seriovy port - napr. druhy koniec virtualneho paru COM portov, ktoreho prvy koniec ma INISS.</summary>
    [JsonStringEnumMemberName("serial")] Serial
}

/// <summary>
/// Koncovy bod linky v tvare hodnoty <c>TablePort</c> v INISSe: <c>TCP://host:port</c> a <c>UDP://host:port</c> - simulator
/// pocuva; <c>\\server\PIPE\meno</c> - rura, ktoru vytvori INISS a simulator sa k nej pripoji.
/// </summary>
/// <param name="Transport">TCP, UDP, rura alebo seriovy port</param>
/// <param name="Host">TCP a UDP: adresa rozhrania (<c>0.0.0.0</c> = vsetky); rura: pocitac s INISSom (<c>.</c> = tento);
/// seriovy port: meno portu (<c>COM13</c>)</param>
/// <param name="Port">cislo portu; pri seriovom porte rychlost; pri rure 0</param>
/// <param name="Pipe">meno rury (bez <c>\\.\PIPE\</c>); pri seriovom porte parita, data a stop bity (<c>n,8,1</c>); inak prazdne</param>
public sealed partial record LineEndpoint(LineTransport Transport, string Host, int Port, string Pipe = "")
{
    /// <summary>Prvy port pri automatickom pridelovani: linka N pocuva na porte <c>BasePort + N</c>.</summary>
    public const int BasePort = 47000;

    /// <summary>Predvoleny koncovy bod linky <paramref name="line" /> (TCP na vsetkych rozhraniach).</summary>
    public static LineEndpoint DefaultFor(int line) => new(LineTransport.Tcp, "0.0.0.0", BasePort + line);

    /// <summary>Rozlozi <c>TCP://host:port</c>, <c>UDP://host:port</c> alebo <c>\\server\PIPE\meno</c>; null pri zlom tvare.</summary>
    public static LineEndpoint? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var s = text.Trim();
        if (SerialPattern().Match(s) is { Success: true } serial)
        {
            var baud = serial.Groups[2].Success ? int.Parse(serial.Groups[2].Value, CultureInfo.InvariantCulture) : 9600;
            var framing = serial.Groups[3].Success
                ? string.Create(CultureInfo.InvariantCulture, $"{serial.Groups[3].Value.ToLowerInvariant()},{serial.Groups[4].Value},{serial.Groups[5].Value}")
                : "n,8,1";
            return baud is > 0 and <= 921600 ? new LineEndpoint(LineTransport.Serial, serial.Groups[1].Value.ToUpperInvariant(), baud, framing) : null;
        }

        if (s.StartsWith(@"\\", StringComparison.Ordinal))
        {
            // \\server\PIPE\meno - INISS rovnako hlada "\PIPE\" bez ohladu na velkost pismen
            var parts = s[2..].Split('\\', 3);
            if (parts.Length != 3 || parts[0].Length == 0 || !parts[1].Equals("PIPE", StringComparison.OrdinalIgnoreCase) || parts[2].Length == 0)
                return null;
            return new LineEndpoint(LineTransport.Pipe, parts[0], 0, parts[2]);
        }

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
    /// Hodnota <c>TablePort</c> pre INISS: <c>N=TCP://host:port</c>, pri rure <c>N=\\.\PIPE\meno</c> (ruru vytvara INISS
    /// na svojom pocitaci).
    /// </summary>
    /// <param name="line">cislo linky v INISSe</param>
    /// <param name="connectHost">adresa tohto pocitaca z pohladu INISSu; null = adresa rozhrania, pri 0.0.0.0 127.0.0.1</param>
    public string ToInissTablePort(int line, string? connectHost = null)
    {
        // pri seriovom porte ma INISS druhy koniec paru - jeho meno simulator nepozna
        if (Transport == LineTransport.Serial)
            return "";
        if (Transport == LineTransport.Pipe)
            return string.Create(CultureInfo.InvariantCulture, $@"{line}=\\.\PIPE\{Pipe}");

        var host = connectHost ?? (Host is "0.0.0.0" or "::" or "*" ? "127.0.0.1" : Host);
        return string.Create(CultureInfo.InvariantCulture, $"{line}={Scheme}://{host}:{Port}");
    }

    private string Scheme => Transport == LineTransport.Tcp ? "TCP" : "UDP";

    /// <inheritdoc />
    public override string ToString() => Transport switch
    {
        LineTransport.Pipe => $@"\\{Host}\PIPE\{Pipe}",
        LineTransport.Serial => string.Create(CultureInfo.InvariantCulture, $"{Host}:{Port},{Pipe}"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{Scheme}://{Host}:{Port}")
    };

    /// <summary><c>COMn</c>, volitelne <c>:rychlost</c> a <c>,parita,data,stop</c> (ako <c>TablePortParam</c> INISSu).</summary>
    [GeneratedRegex(@"^(COM\d{1,3})(?::(\d{1,6})(?:,([NOEMS]),([5-8]),(1|1\.5|2))?)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SerialPattern();
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
    [JsonStringEnumMemberName("error")] Error,

    /// <summary>INISS sa odpojil (TCP, rura) - linka caka, kym sa pripoji znova.</summary>
    [JsonStringEnumMemberName("lost")] Lost,

    /// <summary>Predstierany vypadok linky (scenar porucha) - spojenie je zavrete a nove sa neprijima.</summary>
    [JsonStringEnumMemberName("outage")] Outage
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

    /// <summary>Stav pocitadiel - prehratie naprazdno ich na konci vrati.</summary>
    internal (long BytesIn, long BytesOut, long Frames, long BadFrames, long Discarded, long Connections) Save() =>
        (BytesIn, BytesOut, Frames, BadFrames, Discarded, Connections);

    internal void Restore((long BytesIn, long BytesOut, long Frames, long BadFrames, long Discarded, long Connections) saved) =>
        (BytesIn, BytesOut, Frames, BadFrames, Discarded, Connections) = saved;
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

    /// <summary>Kam sa kopiruju spravy od INISSu (seriovy port, napr. pre ELEN Monitor); null = nikam.</summary>
    public LineEndpoint? Mirror { get; set; }

    /// <summary>Chyba zrkadla (port neexistuje, je obsadeny…); null = v poriadku.</summary>
    public string? MirrorError { get; internal set; }

    /// <summary>Stav spojenia.</summary>
    public LineStatus Status { get; internal set; }

    /// <summary>Podrobnosti stavu - pripojeny klient alebo chyba.</summary>
    public string? StatusDetail { get; internal set; }

    /// <summary>Predstierany vypadok linky - prenos linky nebezi (<see cref="LineStatus.Outage" />).</summary>
    public bool Outage { get; internal set; }

    /// <summary>Od kedy linka je v sucasnom stave.</summary>
    public DateTimeOffset StatusSince { get; internal set; }

    /// <summary>Kedy prisli posledne bajty od INISSu (nie zo zaznamu); null = este nic.</summary>
    public DateTimeOffset? LastReceivedAt { get; internal set; }

    /// <summary>Pocitadla (pri zmene nastavenia linky ostanu).</summary>
    public LineStats Stats { get; init; } = new();

    /// <summary>Zvysi sa pri kazdej zmene.</summary>
    public long Version { get; internal set; }

    internal ILineProtocol Protocol { get; }
}
