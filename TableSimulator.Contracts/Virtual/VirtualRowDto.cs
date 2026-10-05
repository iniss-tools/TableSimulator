namespace TableSimulator.Contracts.Virtual;

/// <summary>Zaznam tabule - polozky v poradi stlpcov katalogovej tabule INISSu.</summary>
public sealed record VirtualRowDto(IReadOnlyList<VirtualCellDto> Cells);
