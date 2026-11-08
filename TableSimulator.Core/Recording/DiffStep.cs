namespace TableSimulator.Core.Recording;

/// <summary>Krok porovnania dvoch priebehov tabule.</summary>
/// <param name="Kind">druh</param>
/// <param name="A">stav v prvom zazname; null pri pridanom</param>
/// <param name="B">stav v druhom zazname; null pri odobranom</param>
public sealed record DiffStep(DiffKind Kind, BoardState? A, BoardState? B);
