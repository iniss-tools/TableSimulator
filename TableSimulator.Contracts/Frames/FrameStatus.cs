using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Frames;

/// <summary>Vysledok spracovania spravy.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FrameStatus>))]
public enum FrameStatus
{
    /// <summary>V poriadku.</summary>
    [JsonStringEnumMemberName("ok")] Ok,

    /// <summary>Zly sucet.</summary>
    [JsonStringEnumMemberName("badChecksum")] BadChecksum,

    /// <summary>Na adrese nie je ziadna tabula.</summary>
    [JsonStringEnumMemberName("noBoard")] NoBoard,

    /// <summary>Odpoved ovplyvnila predstierana porucha.</summary>
    [JsonStringEnumMemberName("fault")] Fault,

    /// <summary>Bajty mimo sprav.</summary>
    [JsonStringEnumMemberName("noise")] Noise,

    /// <summary>V obsahu je bajt konca spravy - tabula ju odmietne.</summary>
    [JsonStringEnumMemberName("eotInData")] EotInData
}
