using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Signage;

/// <summary>Velkost pisma odseku (ako male/stredne/velke v INISSView 2).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SignageTextSize>))]
public enum SignageTextSize
{
    [JsonStringEnumMemberName("small")] Small,
    [JsonStringEnumMemberName("medium")] Medium,
    [JsonStringEnumMemberName("large")] Large
}
