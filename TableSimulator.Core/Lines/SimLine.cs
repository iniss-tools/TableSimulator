using System.Globalization;
using System.Text.Json.Serialization;
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
    [JsonStringEnumMemberName("pipe")] Pipe
}

/// <summary>
/// Koncovy bod linky v tvare hodnoty <c>TablePort</c> v INISSe: <c>TCP://host:port</c> a <c>UDP://host:port</c> - simulator
/// pocuva; <c>\\server\PIPE\meno</c> - rura, ktoru vytvori INISS a simulator sa k nej pripoji.
/// </summary>
/// <param name="Transport">TCP, UDP alebo rura</param>
/// <param name="Host">TCP a UDP: adresa rozhrania (<c>0.0.0.0</c> = vsetky); rura: pocitac s INISSom (<c>.</c> = tento)</param>
/// <param name="Port">cislo portu; pri rure 0</param>
/// <param name="Pipe">meno rury (bez <c>\\.\PIPE\</c>); inak prazdne</param>
public sealed record LineEndpoint(LineTransport Transport, string Host, int Port, string Pipe = "")
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
        if (Transport == LineTransport.Pipe)
            return string.Create(CultureInfo.InvariantCulture, $@"{line}=\\.\PIPE\{Pipe}");

        var host = connectHost ?? (Host is "0.0.0.0" or "::" or "*" ? "127.0.0.1" : Host);
        return string.Create(CultureInfo.InvariantCulture, $"{line}={Scheme}://{host}:{Port}");
    }

    private string Scheme => Transport == LineTransport.Tcp ? "TCP" : "UDP";

    /// <inheritdoc />
    public override string ToString() => Transport == LineTransport.Pipe
        ? $@"\\{Host}\PIPE\{Pipe}"
        : string.Create(CultureInfo.InvariantCulture, $"{Scheme}://{Host}:{Port}");
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
