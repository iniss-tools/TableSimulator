namespace TableSimulator.Contracts.Display;

/// <summary>Riadok tabule ELEN; <see cref="Row" /> cislovany odspodu.</summary>
public sealed record RowDto(int Row, ElementDto[] Elements);
