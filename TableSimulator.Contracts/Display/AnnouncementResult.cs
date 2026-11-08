using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Display;

/// <summary>Ako hlasenie skoncilo.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AnnouncementResult>))]
public enum AnnouncementResult
{
    /// <summary>Este hra.</summary>
    [JsonStringEnumMemberName("playing")] Playing,

    /// <summary>Dohralo.</summary>
    [JsonStringEnumMemberName("completed")] Completed,

    /// <summary>INISS ho zastavil.</summary>
    [JsonStringEnumMemberName("stopped")] Stopped,

    /// <summary>Zrusilo ho hlasenie s vyssou prioritou.</summary>
    [JsonStringEnumMemberName("cancelled")] Cancelled
}
