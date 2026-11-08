namespace TableSimulator.Core.Scenarios;

/// <summary>
/// Scenar porucha v case: kroky od spustenia, raz alebo dookola. Dlzka cyklu je zadana, inak koniec posledneho kroku.
/// </summary>
/// <param name="Name">nazov</param>
/// <param name="Loop">po skonceni cyklu zacne znova</param>
/// <param name="CycleMs">dlzka cyklu [ms]; null = koniec posledneho kroku</param>
/// <param name="Steps">kroky</param>
public sealed record FaultScenario(string Name, bool Loop, int? CycleMs, IReadOnlyList<ScenarioStep> Steps)
{
    /// <summary>Najkratsi cyklus - aby sa scenar bez krokov netocil naprazdno.</summary>
    public const int MinCycleMs = 1000;

    /// <summary>Dlzka jedneho cyklu [ms].</summary>
    public long LengthMs => Math.Max(MinCycleMs, CycleMs ?? (Steps.Count == 0 ? 0 : Steps.Max(s => s.EndMs)));
}
