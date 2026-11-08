using System.Text.Json.Serialization;

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
