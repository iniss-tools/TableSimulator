using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Simulation;

/// <summary>
/// Nastavenie tabule.
/// </summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa na linke</param>
/// <param name="Manufacturer">kluc vyrobcu (<c>MANUFACTURER_KEY</c>)</param>
public sealed record BoardConfig(int Line, int Address, string Manufacturer)
{
    public string Name { get; init; } = "";
    public BoardProfile Profile { get; init; } = new();
    public string? Design { get; init; }
    public BoardFault Fault { get; init; } = BoardFault.None;

    /// <summary>Druha adresa panelu (dvojita adresa) - ovlada hornu polovicu riadkov; null = len jedna adresa.</summary>
    public int? SecondAddress { get; init; }
}
