using System.Text.Json.Serialization;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Scenarios;

/// <summary>Co krok scenara robi.</summary>
public enum ScenarioAction
{
    /// <summary>Predstierana porucha tabule (<see cref="ScenarioStep.Fault" />).</summary>
    [JsonStringEnumMemberName("fault")] Fault,

    /// <summary>Vypadok napajania tabule - neodpoveda a po navrate je prazdna.</summary>
    [JsonStringEnumMemberName("powerLoss")] PowerLoss,

    /// <summary>Vypadok linky - spojenie s INISSom sa zavrie a nove sa neprijima.</summary>
    [JsonStringEnumMemberName("lineOutage")] LineOutage
}

/// <summary>
/// Krok scenara: od <paramref name="AtMs" /> po dobu <paramref name="DurationMs" /> od zaciatku (cyklu) scenara plati
/// akcia pre tabulu linky, vsetky tabule linky alebo linku samu.
/// </summary>
/// <param name="AtMs">zaciatok od zaciatku cyklu [ms]</param>
/// <param name="DurationMs">ako dlho plati [ms]</param>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa tabule; null = vsetky tabule linky (pri vypadku linky sa neberie do uvahy)</param>
/// <param name="Action">co krok robi</param>
public sealed record ScenarioStep(int AtMs, int DurationMs, int Line, int? Address, ScenarioAction Action)
{
    /// <summary>Porucha pri <see cref="ScenarioAction.Fault" />.</summary>
    public BoardFault Fault { get; init; } = BoardFault.None;

    /// <summary>Koniec kroku [ms].</summary>
    public long EndMs => (long)AtMs + DurationMs;
}

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

/// <summary>Stav scenara v case od spustenia.</summary>
/// <param name="Finished">scenar bez opakovania dobehol</param>
/// <param name="Cycle">cislo cyklu od 0</param>
/// <param name="PositionMs">cas v cykle [ms]</param>
/// <param name="ActiveSteps">indexy krokov, ktore teraz platia, v poradi scenara</param>
public sealed record ScenarioMoment(bool Finished, long Cycle, long PositionMs, IReadOnlyList<int> ActiveSteps);

/// <summary>Co ma pocas behu scenara platit - ciste vypocty bez simulatora.</summary>
public static class ScenarioTimeline
{
    /// <summary>Stav scenara <paramref name="elapsed" /> po spusteni.</summary>
    public static ScenarioMoment At(FaultScenario scenario, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var ms = Math.Max(0, (long)elapsed.TotalMilliseconds);
        var length = scenario.LengthMs;
        if (!scenario.Loop && ms >= length)
            return new ScenarioMoment(true, 0, length, []);

        var cycle = ms / length;
        var position = ms % length;
        var active = new List<int>();
        for (var i = 0; i < scenario.Steps.Count; i++)
        {
            var step = scenario.Steps[i];
            if (step.AtMs <= position && position < step.EndMs)
                active.Add(i);
        }

        return new ScenarioMoment(false, cycle, position, active);
    }

    /// <summary>
    /// Ucinok platnych krokov: porucha kazdej tabule (neskorsi krok scenara prepise skorsi), tabule bez napajania a linky
    /// s vypadkom. Krok pre vsetky tabule linky sa rozvinie podla <paramref name="boardsOfLine" />.
    /// </summary>
    public static ScenarioEffects Effects(FaultScenario scenario, IReadOnlyList<int> activeSteps, Func<int, IEnumerable<string>> boardsOfLine)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(activeSteps);
        ArgumentNullException.ThrowIfNull(boardsOfLine);
        var faults = new Dictionary<string, BoardFault>(StringComparer.Ordinal);
        var powerOff = new HashSet<string>(StringComparer.Ordinal);
        var outages = new HashSet<int>();
        foreach (var step in activeSteps.Select(i => scenario.Steps[i]))
        {
            if (step.Action == ScenarioAction.LineOutage)
            {
                outages.Add(step.Line);
                continue;
            }

            var boards = step.Address is { } address ? [SimBoard.MakeId(step.Line, address)] : boardsOfLine(step.Line);
            foreach (var board in boards)
            {
                if (step.Action == ScenarioAction.PowerLoss)
                    powerOff.Add(board);
                else
                    faults[board] = step.Fault;
            }
        }

        return new ScenarioEffects(faults, powerOff, outages);
    }
}

/// <summary>Co scenar v danej chvili nastavuje.</summary>
/// <param name="Faults">porucha tabule podla identifikatora</param>
/// <param name="PoweredOff">tabule bez napajania</param>
/// <param name="Outages">linky s vypadkom</param>
public sealed record ScenarioEffects(IReadOnlyDictionary<string, BoardFault> Faults, IReadOnlySet<string> PoweredOff, IReadOnlySet<int> Outages);
