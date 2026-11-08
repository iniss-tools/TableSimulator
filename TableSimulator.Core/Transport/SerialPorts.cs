using System.Globalization;
using System.IO.Ports;
using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Transport;

/// <summary>
/// Otvaranie seriovych portov podla koncoveho bodu linky.
/// </summary>
public static class SerialPorts
{
    /// <summary>Otvori seriovy port <c>COMn:rychlost,parita,data,stop</c>.</summary>
    public static SerialPort Open(LineEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var parts = endpoint.Pipe.Split(',');
        var parity = (parts.ElementAtOrDefault(0) ?? "n") switch
        {
            "o" => Parity.Odd,
            "e" => Parity.Even,
            "m" => Parity.Mark,
            "s" => Parity.Space,
            _ => Parity.None
        };
        var dataBits = int.TryParse(parts.ElementAtOrDefault(1), NumberStyles.None, CultureInfo.InvariantCulture, out var d) ? d : 8;
        var stopBits = parts.ElementAtOrDefault(2) switch
        {
            "2" => StopBits.Two,
            "1.5" => StopBits.OnePointFive,
            _ => StopBits.One
        };
        var port = new SerialPort(endpoint.Host, endpoint.Port, parity, dataBits, stopBits);
        port.Open();
        return port;
    }
}
