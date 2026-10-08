using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Station;

/// <summary>Zavaznost zistenia o konfiguracii INISSu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<InissProblemSeverity>))]
public enum InissProblemSeverity
{
    [JsonStringEnumMemberName("info")] Info,
    [JsonStringEnumMemberName("warning")] Warning,
    [JsonStringEnumMemberName("error")] Error
}
