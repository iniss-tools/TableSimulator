namespace TableSimulator.Core.Protocols;

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
