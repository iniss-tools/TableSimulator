using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Recordings;

/// <summary>Vysledok porovnania tabule.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BoardDifference>))]
public enum BoardDifference
{
    /// <summary>Tabula ukazovala v oboch zaznamoch to iste v tom istom poradi.</summary>
    [JsonStringEnumMemberName("same")] Same,

    /// <summary>Priebehy sa lisia.</summary>
    [JsonStringEnumMemberName("different")] Different,

    /// <summary>Tabula ukazovala nieco len v prvom zazname.</summary>
    [JsonStringEnumMemberName("onlyA")] OnlyA,

    /// <summary>Tabula ukazovala nieco len v druhom zazname.</summary>
    [JsonStringEnumMemberName("onlyB")] OnlyB
}
