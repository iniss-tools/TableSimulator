using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Recordings;

/// <summary>Krok porovnania priebehu tabule.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ComparisonStepKind>))]
public enum ComparisonStepKind
{
    /// <summary>Rovnaky text v oboch zaznamoch.</summary>
    [JsonStringEnumMemberName("same")] Same,

    /// <summary>Na tom istom mieste postupnosti iny text.</summary>
    [JsonStringEnumMemberName("changed")] Changed,

    /// <summary>Text len v prvom zazname.</summary>
    [JsonStringEnumMemberName("removed")] Removed,

    /// <summary>Text len v druhom zazname.</summary>
    [JsonStringEnumMemberName("added")] Added
}
