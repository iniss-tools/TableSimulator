namespace TableSimulator.Contracts.Scenarios;

/// <summary>Scenar porucha s cislom a stavom behu.</summary>
/// <param name="Id">cislo</param>
/// <param name="Name">nazov</param>
/// <param name="Loop">po skonceni cyklu zacne znova</param>
/// <param name="CycleMs">dlzka cyklu [ms]; null = koniec posledneho kroku</param>
/// <param name="Steps">kroky</param>
/// <param name="Run">beh; null = nebezi</param>
public sealed record FaultScenarioDto(int Id, string Name, bool Loop, int? CycleMs, IReadOnlyList<ScenarioStepDto> Steps, ScenarioRunDto? Run);
