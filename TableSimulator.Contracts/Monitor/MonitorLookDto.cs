namespace TableSimulator.Contracts.Monitor;

/// <summary>Farby a pismo programu ELEN Monitor (<c>#rrggbb</c>).</summary>
public sealed record MonitorLookDto(string FontName, string FontColor, string HeaderColor, string DisplayColor);
