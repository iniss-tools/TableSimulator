using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.AkSignal;

/// <summary>
/// Ako snimac tmy rozhoduje, ci je tma.
/// </summary>
public enum TwilightMode
{
    /// <summary>Podla vychodu a zapadu slnka v polohe simulatora.</summary>
    [JsonStringEnumMemberName("sun")] Sun,

    /// <summary>Stale tma.</summary>
    [JsonStringEnumMemberName("dark")] Dark,

    /// <summary>Stale svetlo.</summary>
    [JsonStringEnumMemberName("light")] Light
}
