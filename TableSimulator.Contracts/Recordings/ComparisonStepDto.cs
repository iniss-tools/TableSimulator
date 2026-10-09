namespace TableSimulator.Contracts.Recordings;

/// <summary>Krok porovnania: stav v prvom a druhom zazname.</summary>
/// <param name="A">stav v prvom zazname; null pri pridanom</param>
/// <param name="B">stav v druhom zazname; null pri odobranom</param>
public sealed record ComparisonStepDto(ComparisonStepKind Kind, BoardStateDto? A, BoardStateDto? B);
