using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Simulation;

/// <summary>Linka stanice.</summary>
/// <param name="Number">cislo linky (podla neho INISS priraduje tabule)</param>
/// <param name="TableClass">trieda linky</param>
/// <param name="Port">port, na ktory INISS posiela (TablePort <c>N=TCP://host:port</c>)</param>
public sealed record StationLine(int Number, int TableClass, int Port)
{
    /// <summary>Nazov pre obsluhu (napr. sekcia Driver0).</summary>
    public string Name { get; init; } = "";

    /// <summary>Ako INISS na linku posiela.</summary>
    public LineTransport Transport { get; init; } = LineTransport.Tcp;
}
