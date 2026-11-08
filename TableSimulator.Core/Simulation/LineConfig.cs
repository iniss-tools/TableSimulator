using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Simulation;

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
