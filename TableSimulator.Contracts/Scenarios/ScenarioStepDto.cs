using TableSimulator.Contracts.Boards;

namespace TableSimulator.Contracts.Scenarios;

/// <summary>Krok scenara: od <paramref name="AtMs" /> po dobu <paramref name="DurationMs" /> plati akcia.</summary>
/// <param name="AtMs">zaciatok od zaciatku cyklu [ms]</param>
/// <param name="DurationMs">ako dlho plati [ms]</param>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa tabule; null = vsetky tabule linky (pri vypadku linky sa neberie do uvahy)</param>
/// <param name="Action">co krok robi</param>
/// <param name="Fault">porucha pri <see cref="ScenarioAction.Fault" />; inak null</param>
public sealed record ScenarioStepDto(int AtMs, int DurationMs, int Line, int? Address, ScenarioAction Action, BoardFaultDto? Fault);
