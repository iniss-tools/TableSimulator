namespace TableSimulator.Application.Virtual;

/// <summary>Kolko tabul import pridal, prepisal a vynechal.</summary>
public readonly record struct VirtualImportCounts(int Added, int Replaced, int Skipped);
