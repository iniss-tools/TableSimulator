using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;

namespace TableSimulator.Core.Scenarios;

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
    /// Ucinok platnych krokov: porucha kazdej tabule (neskorsi krok scenara prepise skorsi), tabule bez napajania, linky
    /// s vypadkom a prekrytie ustredni (stavy z vsetkych krokov, snimac z neskorsieho). Krok pre vsetky tabule linky sa
    /// rozvinie podla <paramref name="boardsOfLine" />.
    /// </summary>
    public static ScenarioEffects Effects(FaultScenario scenario, IReadOnlyList<int> activeSteps, Func<int, IEnumerable<string>> boardsOfLine)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(activeSteps);
        ArgumentNullException.ThrowIfNull(boardsOfLine);
        var faults = new Dictionary<string, BoardFault>(StringComparer.Ordinal);
        var powerOff = new HashSet<string>(StringComparer.Ordinal);
        var outages = new HashSet<int>();
        var devices = new Dictionary<string, AkSignalOverride>(StringComparer.Ordinal);
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
                else if (step.Action == ScenarioAction.Device)
                    devices[board] = devices.TryGetValue(board, out var earlier) ? earlier.Then(step.Device) : step.Device;
                else
                    faults[board] = step.Fault;
            }
        }

        return new ScenarioEffects(faults, powerOff, outages) { Devices = devices };
    }
}
