using System.Text.Json.Serialization;

namespace TableSimulator.Core.Lines;

/// <summary>
/// Rezim linky.
/// </summary>
public enum LineMode
{
    /// <summary>Tabule odpovedaju ako skutocne.</summary>
    [JsonStringEnumMemberName("display")] Display,

    /// <summary>Tabule len pocuvaju a neodpovedaju (skutocne tabule na linke odpovedaju samy).</summary>
    [JsonStringEnumMemberName("monitor")] Monitor
}
