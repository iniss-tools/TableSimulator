using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Station;

/// <summary>Zistenie o linke alebo tabuli konfiguracie INISSu (texty s parametrami <see cref="InissProblemDto.Args" /> ma web).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<InissProblemCode>))]
public enum InissProblemCode
{
    /// <summary>Cislo linky mimo 1-100 (parameter: cislo).</summary>
    [JsonStringEnumMemberName("lineOutOfRange")] LineOutOfRange,

    /// <summary>Cislo linky uz pouziva skorsia sekcia (parametre: cislo, sekcia).</summary>
    [JsonStringEnumMemberName("lineDuplicate")] LineDuplicate,

    /// <summary>Tabula nepatri na linku s protokolom (parametre: tabula, vyrobca, trieda).</summary>
    [JsonStringEnumMemberName("wrongFamily")] WrongFamily,

    /// <summary>Linka nema ziadnu tabulu.</summary>
    [JsonStringEnumMemberName("noTables")] NoTables,

    /// <summary>Tabula s automatickym priradenim nema linku s vhodnym protokolom.</summary>
    [JsonStringEnumMemberName("noAutomaticLine")] NoAutomaticLine,

    /// <summary>Linku tabule neobsluhuje ziadna sekcia Driver (parameter: cislo linky).</summary>
    [JsonStringEnumMemberName("noDriver")] NoDriver
}
