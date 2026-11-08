namespace TableSimulator.Application.Statistics;

/// <summary>Zmena stavu linky v databaze; stav je kluc z JSON (<c>connected</c>, <c>lost</c>…).</summary>
public sealed record LineStatusRecord(long Id, int Line, long TimeMs, string Status, string? Detail);
