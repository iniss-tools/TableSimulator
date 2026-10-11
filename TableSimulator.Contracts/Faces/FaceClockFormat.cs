using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Faces;

/// <summary>Format hodin cela.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FaceClockFormat>))]
public enum FaceClockFormat
{
    /// <summary>9:05</summary>
    [JsonStringEnumMemberName("H:mm")] HourMinute,

    /// <summary>09:05</summary>
    [JsonStringEnumMemberName("HH:mm")] HourMinutePadded,

    /// <summary>09:05:30</summary>
    [JsonStringEnumMemberName("HH:mm:ss")] WithSeconds
}
