using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.Faces;

/// <summary>Format hodin cela.</summary>
public enum FaceClockFormat
{
    /// <summary>9:05</summary>
    [JsonStringEnumMemberName("H:mm")] HourMinute,

    /// <summary>09:05</summary>
    [JsonStringEnumMemberName("HH:mm")] HourMinutePadded,

    /// <summary>09:05:30</summary>
    [JsonStringEnumMemberName("HH:mm:ss")] WithSeconds
}
