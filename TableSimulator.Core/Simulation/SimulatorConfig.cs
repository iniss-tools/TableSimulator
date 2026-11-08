using TableSimulator.Core.Boards.Faces;

namespace TableSimulator.Core.Simulation;

/// <summary>
/// Ulozene nastavenie liniek a tabul simulatora.
/// </summary>
public sealed record SimulatorConfig
{
    public List<LineConfig> Lines { get; init; } = [];
    public List<BoardConfig> Boards { get; init; } = [];

    /// <summary>
    /// Vlastne predlohy ciel, ktore tabule pouzivaju - len v subore konfiguracie (export a import); simulator ich
    /// nedrzi, su v databaze servera.
    /// </summary>
    public List<BoardFace> Faces { get; init; } = [];
}
