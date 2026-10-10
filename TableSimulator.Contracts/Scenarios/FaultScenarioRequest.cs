namespace TableSimulator.Contracts.Scenarios;

/// <summary>Scenar porucha (telo POST /api/scenarios a PUT /api/scenarios/{id}).</summary>
/// <param name="Name">nazov</param>
/// <param name="Loop">po skonceni cyklu zacne znova</param>
/// <param name="CycleMs">dlzka cyklu [ms]; null = koniec posledneho kroku</param>
/// <param name="Steps">kroky</param>
public sealed record FaultScenarioRequest(string Name, bool Loop, int? CycleMs, IReadOnlyList<ScenarioStepDto> Steps);
