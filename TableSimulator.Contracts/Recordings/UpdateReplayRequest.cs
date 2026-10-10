namespace TableSimulator.Contracts.Recordings;

/// <summary>Zmena prebiehajuceho prehravania (PUT /api/replay); null = nemenit.</summary>
/// <param name="Paused">pozastavit alebo pokracovat (dohrane zacne od zaciatku)</param>
/// <param name="Position">posunut na miesto [s]</param>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Loop">opakovat</param>
public sealed record UpdateReplayRequest(bool? Paused, double? Position, double? Speed, bool? Loop);
