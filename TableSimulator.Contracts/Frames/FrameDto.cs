namespace TableSimulator.Contracts.Frames;

/// <summary>Zaznam spravy na linke - bajty ako hex, vyznam ako druh s parametrami (texty sklada rozhranie).</summary>
public sealed record FrameDto(
    long Seq,
    DateTimeOffset Time,
    int Line,
    FrameDirection Direction,
    int? Address,
    string Hex,
    FrameStatus Status,
    string Kind,
    IReadOnlyDictionary<string, string>? Args);
