using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Mcp.Results;

/// <summary>Scenare poruch.</summary>
public sealed record ScenariosResult(IReadOnlyList<FaultScenarioDto> Scenarios);
