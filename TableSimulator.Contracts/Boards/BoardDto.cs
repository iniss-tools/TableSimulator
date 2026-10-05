using TableSimulator.Contracts.Display;

namespace TableSimulator.Contracts.Boards;

/// <summary>Tabula pre webove rozhranie.</summary>
/// <param name="Id">identifikator <c>linka-adresa</c></param>
public sealed record BoardDto(
    string Id,
    int Line,
    int Address,
    string Name,
    string Manufacturer,
    BoardProfileDto Profile,
    string? Design,
    BoardFaultDto Fault,
    bool AutoCreated,
    BoardStatsDto Stats,
    DisplayDto Display,
    long Version)
{
    /// <summary>Druha adresa panelu; null = len jedna.</summary>
    public int? SecondAddress { get; init; }

    /// <summary>Spravy od INISSu patria inemu vyrobcovi; null = v poriadku.</summary>
    public ManufacturerMismatchDto? Mismatch { get; init; }
}
