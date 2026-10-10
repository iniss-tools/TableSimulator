namespace TableSimulator.Contracts.Scenarios;

/// <summary>Beziaci scenar.</summary>
/// <param name="StartedAt">kedy sa spustil</param>
/// <param name="LengthMs">dlzka cyklu [ms]</param>
/// <param name="Cycle">cislo cyklu od 0</param>
/// <param name="ActiveSteps">indexy krokov, ktore teraz platia</param>
public sealed record ScenarioRunDto(DateTimeOffset StartedAt, long LengthMs, long Cycle, IReadOnlyList<int> ActiveSteps);
