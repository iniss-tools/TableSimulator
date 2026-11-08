using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.Announcements;

/// <summary>Ako hlasenie skoncilo.</summary>
public enum AnnouncementResult
{
    /// <summary>Este hra.</summary>
    [JsonStringEnumMemberName("playing")] Playing,

    /// <summary>Dohralo.</summary>
    [JsonStringEnumMemberName("completed")] Completed,

    /// <summary>INISS ho zastavil.</summary>
    [JsonStringEnumMemberName("stopped")] Stopped,

    /// <summary>Zrusilo ho hlasenie s vyssou prioritou alebo porucha zariadenia.</summary>
    [JsonStringEnumMemberName("cancelled")] Cancelled
}
