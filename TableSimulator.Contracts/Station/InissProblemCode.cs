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
    [JsonStringEnumMemberName("noDriver")] NoDriver,
    /// <summary>INISS2: linka tabule nie je v komunikacnych linkach servera (parameter: kluc linky).</summary>
    [JsonStringEnumMemberName("unknownLine")] UnknownLine,

    /// <summary>INISS2: linka ustredne AK Signal nema svetlo ani audio linku.</summary>
    [JsonStringEnumMemberName("noDevices")] NoDevices,

    /// <summary>INISS2: svetlo alebo audio linka bez linky a ziadna linka s vhodnym protokolom (parametre: nazov, protokol).</summary>
    [JsonStringEnumMemberName("deviceNoLine")] DeviceNoLine,

    /// <summary>INISS2: svetlo bez linky a viac liniek s vhodnym protokolom (parametre: nazov, protokol).</summary>
    [JsonStringEnumMemberName("deviceLineAmbiguous")] DeviceLineAmbiguous,

    /// <summary>INISS2: linka svetla alebo audio linky nie je v CommLines.xml (parametre: nazov, kluc linky).</summary>
    [JsonStringEnumMemberName("deviceUnknownLine")] DeviceUnknownLine,

    /// <summary>INISS2: linka svetla alebo audio linky ma iny protokol (parametre: nazov, kluc linky, protokol linky).</summary>
    [JsonStringEnumMemberName("deviceWrongProtocol")] DeviceWrongProtocol,

    /// <summary>INISS2: adresa ustredne AK Signal mimo 1-255 (parametre: nazov, adresa).</summary>
    [JsonStringEnumMemberName("deviceAddress")] DeviceAddress,

    /// <summary>INISS2: kanal ustredne AK Signal UH mimo 1-2 (parametre: nazov, kanal).</summary>
    [JsonStringEnumMemberName("audioChannel")] AudioChannel,

    /// <summary>INISS2: neznamy druh ustredne v Lights.xml (parametre: nazov, druh).</summary>
    [JsonStringEnumMemberName("deviceType")] DeviceType,

    /// <summary>INISS2: svetlo alebo audio linka na linke MASTER_LIGHT - simulator ju nehra (parametre: nazov, kluc linky).</summary>
    [JsonStringEnumMemberName("masterLight")] MasterLight
}
