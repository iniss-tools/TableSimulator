namespace TableSimulator.Core.Boards.AkSignal;

/// <summary>
/// Snimac tmy ustredne.
/// </summary>
public sealed record TwilightSensor
{
    /// <summary>Ako snimac rozhoduje.</summary>
    public TwilightMode Mode { get; init; } = TwilightMode.Sun;

    /// <summary>
    /// Posun voci vychodu a zapadu [min]: tma zacne tolko minut po zapade a skonci tolko minut pred vychodom; zaporny
    /// posun tmu predlzi.
    /// </summary>
    public int OffsetMinutes { get; init; }
}
