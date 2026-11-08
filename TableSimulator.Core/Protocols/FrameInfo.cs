namespace TableSimulator.Core.Protocols;

/// <summary>
/// Rozlozeny vyznam spravy - texty pre obsluhu sklada az webove rozhranie podla <see cref="Kind" />.
/// </summary>
/// <param name="Kind">druh spravy, napr. <c>elen.row</c>, <c>ack</c></param>
/// <param name="Args">parametre na zobrazenie (riadok, text…)</param>
public sealed record FrameInfo(string Kind, IReadOnlyDictionary<string, string>? Args = null);
