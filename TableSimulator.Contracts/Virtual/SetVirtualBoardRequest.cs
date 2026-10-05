namespace TableSimulator.Contracts.Virtual;

/// <summary>Zalozenie alebo zmena virtualnej tabule (PUT /api/virtual/{cislo}).</summary>
public sealed record SetVirtualBoardRequest
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Source { get; init; } = "";
    public int? RowCount { get; init; }
    public int RowStart { get; init; } = 1;
    public string Separator { get; init; } = "";
    public string Template { get; init; } = "";
    public string Parameter { get; init; } = "";
}
