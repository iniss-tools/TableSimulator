namespace TableSimulator.Application.Statistics;

/// <summary>Uchovavanie a intervaly grafov statistiky.</summary>
public static class TrafficRetention
{
    /// <summary>Ako dlho sa statistika uchovava [dni].</summary>
    public const int Days = 30;

    /// <summary>Najdlhsie obdobie jedneho dotazu [dni].</summary>
    public const int MaxSpanDays = 31;

    /// <summary>Dlzka intervalu grafu podla obdobia - do 3 h po minute, do 2 dni po 10 min, do 8 dni po hodine, inak po 6 h.</summary>
    public static int BucketFor(TimeSpan span) =>
        span <= TimeSpan.FromHours(3) ? 1 : span <= TimeSpan.FromDays(2) ? 10 : span <= TimeSpan.FromDays(8) ? 60 : 360;
}
