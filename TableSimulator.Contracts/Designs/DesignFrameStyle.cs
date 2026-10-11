using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Designs;

/// <summary>Druh ramu tabule.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DesignFrameStyle>))]
public enum DesignFrameStyle
{
    /// <summary>Plocha farba.</summary>
    [JsonStringEnumMemberName("flat")] Flat,

    /// <summary>Kovovy profil - prechod svetlejsej a tmavsej farby s lesklou hranou.</summary>
    [JsonStringEnumMemberName("metal")] Metal
}
