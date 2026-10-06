using System.Text.Json.Serialization;

namespace TableSimulator.Core.Protocols;

/// <summary>
/// Smer bajtov na linke.
/// </summary>
public enum FrameDirection
{
    /// <summary>Od INISSu k tabuli.</summary>
    [JsonStringEnumMemberName("rx")] Received,

    /// <summary>Od tabule (simulatora) k INISSu.</summary>
    [JsonStringEnumMemberName("tx")] Sent
}

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

/// <summary>
/// Rozlozeny vyznam spravy - texty pre obsluhu sklada az webove rozhranie podla <see cref="Kind" />.
/// </summary>
/// <param name="Kind">druh spravy, napr. <c>elen.row</c>, <c>ack</c></param>
/// <param name="Args">parametre na zobrazenie (riadok, text…)</param>
public sealed record FrameInfo(string Kind, IReadOnlyDictionary<string, string>? Args = null);

/// <summary>
/// Zaznam jednej spravy alebo odpovede na linke.
/// </summary>
/// <param name="Seq">poradove cislo zaznamu</param>
/// <param name="Time">cas</param>
/// <param name="Line">cislo linky</param>
/// <param name="Direction">smer</param>
/// <param name="Address">adresa tabule; null = nezistena</param>
/// <param name="Bytes">bajty</param>
/// <param name="Status">vysledok</param>
/// <param name="Info">rozlozeny vyznam</param>
public sealed record FrameRecord(
    long Seq,
    DateTimeOffset Time,
    int Line,
    FrameDirection Direction,
    int? Address,
    byte[] Bytes,
    FrameStatus Status,
    FrameInfo Info);
