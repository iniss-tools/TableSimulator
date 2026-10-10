namespace TableSimulator.Contracts.Recordings;

/// <summary>Prehratie zaznamu (POST /api/replay).</summary>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Loop">dokola</param>
public sealed record PlayRequest(string Name, double Speed, bool Loop)
{
    /// <summary>Odkial prehravat [s]; od ineho miesta nez od zaciatku sa zaznam najprv prehra naprazdno.</summary>
    public double Position { get; init; }

    /// <summary>Len pripravit na mieste a pozastavit.</summary>
    public bool Paused { get; init; }
}
