using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Station;

/// <summary>Jazyk popisov na celach tabul, ktore simulator zalozi z dat stanice (druhy riadok je vzdy anglicky).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StationFaceLanguage>))]
public enum StationFaceLanguage
{
    /// <summary>Slovensky.</summary>
    [JsonStringEnumMemberName("sk")] Sk,

    /// <summary>Cesky.</summary>
    [JsonStringEnumMemberName("cs")] Cs
}
