using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.AkSignal;

/// <summary>Udalost v logu svetiel servera INISS2.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ServerLightEvent>))]
public enum ServerLightEvent
{
    /// <summary>Zmena potreby svietit pre vlak.</summary>
    [JsonStringEnumMemberName("train")] Train,

    /// <summary>Rucna zmena rezimu obsluhou.</summary>
    [JsonStringEnumMemberName("mode")] Mode,

    /// <summary>Zmena stavu svetla podla ustredne.</summary>
    [JsonStringEnumMemberName("state")] State,

    /// <summary>Ina udalost.</summary>
    [JsonStringEnumMemberName("other")] Other
}
