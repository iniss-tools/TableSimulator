using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Station;

/// <summary>Co simulator urobil s priecinkom logov INISSu z importu stanice.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StationLogFolder>))]
public enum StationLogFolder
{
    /// <summary>Import priecinok neobsahoval.</summary>
    [JsonStringEnumMemberName("none")] None,

    /// <summary>Simulator ho zacal sledovat.</summary>
    [JsonStringEnumMemberName("added")] Added,

    /// <summary>Simulator ho uz sledoval.</summary>
    [JsonStringEnumMemberName("watched")] Watched,

    /// <summary>Simulator priecinok nevidi (bezi na inom pocitaci alebo priecinok neexistuje).</summary>
    [JsonStringEnumMemberName("missing")] Missing,

    /// <summary>Simulator uz sleduje najvyssi pocet priecinkov.</summary>
    [JsonStringEnumMemberName("full")] Full
}
