using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;

namespace TableSimulator.Core.Scenarios;

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

    /// <summary>Stavy a snimac ustredne pri <see cref="ScenarioAction.Device" />.</summary>
    public AkSignalOverride Device { get; init; } = new(AkSignalConditions.None, null);

    /// <summary>Koniec kroku [ms].</summary>
    public long EndMs => (long)AtMs + DurationMs;
}
