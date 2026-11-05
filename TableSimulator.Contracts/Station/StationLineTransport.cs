using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Station;

/// <summary>Ako server posiela na linku stanice.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StationLineTransport>))]
public enum StationLineTransport
{
    /// <summary>Spojenie TCP.</summary>
    [JsonStringEnumMemberName("tcp")] Tcp,

    /// <summary>Datagramy UDP.</summary>
    [JsonStringEnumMemberName("udp")] Udp
}
