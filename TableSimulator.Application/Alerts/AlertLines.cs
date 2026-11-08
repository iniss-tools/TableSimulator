using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Alerts;

/// <summary>
/// Stav liniek pre oznamenia: stav linky, a ak linka funguje, ale server INISS2 sa niektorej jej ustredne AK Signal
/// dlhsie nepyta (<see cref="AkSignalDisplay.ServerSilent" />), stav <see cref="AlertLineState.Silent" /> s ustrednami
/// v podrobnosti a casom posledneho dotazu.
/// </summary>
public static class AlertLines
{
    /// <summary>Stav vsetkych liniek simulatora.</summary>
    public static IReadOnlyList<AlertLineState> Of(Simulator simulator)
    {
        ArgumentNullException.ThrowIfNull(simulator);
        return simulator.Read(s => s.Lines.Select(l => Of(l, s.Boards.Where(b => b.Line == l.Number))).ToList());
    }

    /// <summary>Stav jednej linky s jej tabulami.</summary>
    public static AlertLineState Of(SimLine line, IEnumerable<SimBoard> boards)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(boards);
        var state = new AlertLineState(line.Number, line.Name, SimulatorConfigJson.Key(line.Status), line.StatusDetail, line.StatusSince);
        if (state.IsOutage)
            return state;

        var silent = boards
            .Where(b => !b.PoweredOff && b.Display is AkSignalDisplay { ServerSilent: true })
            .Select(b => (Board: b, Display: (AkSignalDisplay)b.Display))
            .OrderBy(s => s.Board.Address)
            .ToList();
        if (silent.Count == 0)
            return state;

        var since = silent.Min(s => s.Display.LastQueryAt) ?? line.StatusSince;
        var detail = string.Join(", ", silent.Select(s => s.Board.Name.Length > 0 ? s.Board.Name : s.Board.Address.ToString(CultureInfo.InvariantCulture)));
        return state with { Status = AlertLineState.Silent, Detail = detail, Since = since };
    }
}
