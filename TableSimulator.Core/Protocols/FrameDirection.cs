using System.Text.Json.Serialization;

namespace TableSimulator.Core.Protocols;

/// <summary>
/// Smer bajtov na linke.
/// </summary>
public enum FrameDirection
{
    /// <summary>Od INISSu k tabuli.</summary>
    [JsonStringEnumMemberName("rx")] Received,

    /// <summary>Od tabule (simulatora) k INISSu.</summary>
    [JsonStringEnumMemberName("tx")] Sent
}
