using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Lines;

/// <summary>Rezim linky.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LineMode>))]
public enum LineMode
{
    /// <summary>Tabule odpovedaju ako skutocne.</summary>
    [JsonStringEnumMemberName("display")] Display,

    /// <summary>Tabule len pocuvaju - na linke odpovedaju skutocne tabule.</summary>
    [JsonStringEnumMemberName("monitor")] Monitor
}
