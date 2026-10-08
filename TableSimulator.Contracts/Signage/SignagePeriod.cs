using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Signage;

/// <summary>Kedy playlist plati (ako v INISSView 2); pri prekryti vyhra jeden den pred dnami v tyzdni a tie pred kazdym dnom.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SignagePeriod>))]
public enum SignagePeriod
{
    /// <summary>Len v den zaciatku.</summary>
    [JsonStringEnumMemberName("once")] Once,

    /// <summary>Kazdy den od zaciatku do konca.</summary>
    [JsonStringEnumMemberName("daily")] Daily,

    /// <summary>Vo vybrane dni v tyzdni od zaciatku do konca.</summary>
    [JsonStringEnumMemberName("weekly")] Weekly
}
