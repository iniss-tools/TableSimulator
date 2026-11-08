using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Application.Scenarios;

/// <summary>Scenar porucha v databaze; obsah (<see cref="FaultScenarioRequest" />) ako JSON.</summary>
public sealed record FaultScenarioRecord(int Id, string Name, string Definition);
