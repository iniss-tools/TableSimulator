using System.Text.Json.Serialization;

namespace TableSimulator.Core.Protocols;

/// <summary>
/// Vysledok spracovania spravy.
/// </summary>
public enum FrameStatus
{
    [JsonStringEnumMemberName("ok")] Ok,
    [JsonStringEnumMemberName("badChecksum")] BadChecksum,

    /// <summary>Na adrese nie je ziadna simulovana tabula.</summary>
    [JsonStringEnumMemberName("noBoard")] NoBoard,

    /// <summary>Odpoved ovplyvnila predstierana porucha.</summary>
    [JsonStringEnumMemberName("fault")] Fault,

    /// <summary>Bajty mimo sprav (sum).</summary>
    [JsonStringEnumMemberName("noise")] Noise,

    /// <summary>
    /// V obsahu spravy je bajt konca spravy (ELEN <c>04</c>, napr. cislo pisma bez bitu <c>0x40</c>) - tabula spravu
    /// ukonci skor a odmietne.
    /// </summary>
    [JsonStringEnumMemberName("eotInData")] EotInData
}
