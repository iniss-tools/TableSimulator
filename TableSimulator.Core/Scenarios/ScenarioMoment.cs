namespace TableSimulator.Core.Scenarios;

/// <summary>Stav scenara v case od spustenia.</summary>
/// <param name="Finished">scenar bez opakovania dobehol</param>
/// <param name="Cycle">cislo cyklu od 0</param>
/// <param name="PositionMs">cas v cykle [ms]</param>
/// <param name="ActiveSteps">indexy krokov, ktore teraz platia, v poradi scenara</param>
public sealed record ScenarioMoment(bool Finished, long Cycle, long PositionMs, IReadOnlyList<int> ActiveSteps);
