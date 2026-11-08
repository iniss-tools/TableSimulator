using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Boards;

/// <summary>Ako snimac tmy ustredne AK Signal rozhoduje, ci je tma.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TwilightMode>))]
public enum TwilightMode
{
    /// <summary>Podla vychodu a zapadu slnka v polohe simulatora.</summary>
    [JsonStringEnumMemberName("sun")] Sun,

    /// <summary>Stale tma.</summary>
    [JsonStringEnumMemberName("dark")] Dark,

    /// <summary>Stale svetlo.</summary>
    [JsonStringEnumMemberName("light")] Light
}
