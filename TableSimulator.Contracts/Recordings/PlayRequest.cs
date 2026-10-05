namespace TableSimulator.Contracts.Recordings;

/// <summary>Prehratie zaznamu (POST /api/replay).</summary>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Loop">dokola</param>
public sealed record PlayRequest(string Name, double Speed, bool Loop);
