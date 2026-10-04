using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Simulation;

/// <summary>
/// Ulozene nastavenie liniek a tabul simulatora.
/// </summary>
public sealed record SimulatorConfig
{
    public List<LineConfig> Lines { get; init; } = [];
    public List<BoardConfig> Boards { get; init; } = [];
}

/// <summary>
/// Nastavenie linky.
/// </summary>
/// <param name="Number">cislo linky 1-100</param>
/// <param name="TableClass">trieda linky (rodina protokolu)</param>
/// <param name="Endpoint">kde linka pocuva, <c>TCP://host:port</c></param>
public sealed record LineConfig(int Number, int TableClass, string Endpoint)
{
    public string Name { get; init; } = "";
    public LineMode Mode { get; init; }
    public bool AutoLearn { get; init; } = true;
    public string Manufacturer { get; init; } = "ELEN";

    /// <summary>Seriovy port, kam sa kopiruju spravy od INISSu (<c>COM10:9600,n,8,1</c>); null = nikam.</summary>
    public string? Mirror { get; init; }
}

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
