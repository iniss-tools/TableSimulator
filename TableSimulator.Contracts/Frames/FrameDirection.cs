using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Frames;

/// <summary>Smer bajtov na linke.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FrameDirection>))]
public enum FrameDirection
{
    /// <summary>Od INISSu k tabuli.</summary>
    [JsonStringEnumMemberName("rx")] Received,

    /// <summary>Od tabule k INISSu.</summary>
    [JsonStringEnumMemberName("tx")] Sent
}
