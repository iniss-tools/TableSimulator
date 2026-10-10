using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Recordings;

/// <summary>Stav prehravania zaznamu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReplayState>))]
public enum ReplayState
{
    /// <summary>Prehrava sa.</summary>
    [JsonStringEnumMemberName("running")] Running,

    /// <summary>Pozastavene - caka na pokracovanie alebo posun.</summary>
    [JsonStringEnumMemberName("paused")] Paused,

    /// <summary>Dohralo.</summary>
    [JsonStringEnumMemberName("finished")] Finished,

    /// <summary>Zastavene obsluhou.</summary>
    [JsonStringEnumMemberName("stopped")] Stopped
}
