using System.Text.Json.Serialization;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Stav prehravania.
/// </summary>
public enum ReplayState
{
    [JsonStringEnumMemberName("running")] Running,
    [JsonStringEnumMemberName("paused")] Paused,
    [JsonStringEnumMemberName("finished")] Finished,
    [JsonStringEnumMemberName("stopped")] Stopped
}
