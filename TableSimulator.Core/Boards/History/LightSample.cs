namespace TableSimulator.Core.Boards.History;

/// <summary>Zmena stavu svetla ustredne.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa ustredne</param>
/// <param name="At">cas zmeny</param>
/// <param name="Flags">novy stav</param>
public sealed record LightSample(int Line, int Address, DateTimeOffset At, LightFlags Flags);
