using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Station;

/// <summary>Druh tabule stanice (VIEW_TYPE logickych tabul) - urcuje, ake celo simulator tabuli zalozi.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StationBoardKind>))]
public enum StationBoardKind
{
    /// <summary>Iny alebo neznamy druh - bez cela.</summary>
    [JsonStringEnumMemberName("other")] Other,

    /// <summary>Odchodova tabula.</summary>
    [JsonStringEnumMemberName("departures")] Departures,

    /// <summary>Prichodova tabula.</summary>
    [JsonStringEnumMemberName("arrivals")] Arrivals,

    /// <summary>Nastupistna tabula nad kolajou.</summary>
    [JsonStringEnumMemberName("platform")] Platform,

    /// <summary>Podchodova tabula pri vychode na nastupiste.</summary>
    [JsonStringEnumMemberName("underpass")] Underpass
}
