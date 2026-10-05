namespace TableSimulator.Contracts.Display;

/// <summary>Prvok riadku ELEN: <c>run</c> (usek) alebo <c>alt</c> (striedanie casti).</summary>
public sealed record ElementDto(string Type, RunDto? Run, RunDto[][]? Parts);
