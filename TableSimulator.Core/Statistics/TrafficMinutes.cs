namespace TableSimulator.Core.Statistics;

/// <summary>Minuty statistiky - cislo minuty od 1. 1. 1970 UTC.</summary>
public static class TrafficMinutes
{
    /// <summary>Minuta, do ktorej cas patri.</summary>
    public static long Of(DateTimeOffset time) => Math.DivRem(time.ToUnixTimeMilliseconds(), 60_000, out var rest) - (rest < 0 ? 1 : 0);

    /// <summary>Zaciatok minuty.</summary>
    public static DateTimeOffset Start(long minute) => DateTimeOffset.FromUnixTimeMilliseconds(minute * 60_000);
}
