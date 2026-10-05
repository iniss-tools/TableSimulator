using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Virtual;

/// <summary>Hodiny v hlavicke virtualnej tabule.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<VirtualClock>))]
public enum VirtualClock
{
    /// <summary>Bez hodin.</summary>
    [JsonStringEnumMemberName("none")] None,

    /// <summary>Hodiny a minuty.</summary>
    [JsonStringEnumMemberName("minutes")] Minutes,

    /// <summary>Hodiny, minuty a sekundy.</summary>
    [JsonStringEnumMemberName("seconds")] Seconds
}
