namespace TableSimulator.Core.Boards;

/// <summary>
/// Vychod a zapad slnka podla algoritmu Almanac for Computers (1990) - presnost okolo dvoch minut, na snimac tmy staci.
/// Ciste vypocty bez stavu.
/// </summary>
public static class SunTimes
{
    /// <summary>Uhol slnka pod obzorom pri vychode a zapade (lom svetla a polomer slnka).</summary>
    private const double Zenith = 90.833;

    /// <summary>
    /// Vychod a zapad slnka v den <paramref name="date" /> (UTC). Null = v ten den slnko nevychadza alebo nezapada
    /// (polarny den alebo noc, viz <see cref="IsPolarDay" />).
    /// </summary>
    public static (DateTimeOffset? Rise, DateTimeOffset? Set) For(DateOnly date, GeoLocation location) =>
        (Event(date, location, true), Event(date, location, false));

    /// <summary>
    /// Je tma: od zapadu slnka posunuteho o <paramref name="offsetMinutes" /> neskor po vychod posunuty o tolko isto
    /// skor. Kladny posun tmu skracuje, zaporny predlzuje. Pri polarnom dni nie je tma nikdy, pri polarnej noci stale.
    /// </summary>
    public static bool IsDark(DateTimeOffset now, GeoLocation location, int offsetMinutes = 0)
    {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var day = DateOnly.FromDateTime(now.UtcDateTime);

        // posledna udalost pred casom rozhodne - tri dni pokryju aj posun dna pri velkej zemepisnej dlzke
        (DateTimeOffset At, bool Rise)? last = null;
        for (var shift = -1; shift <= 1; shift++)
        {
            var (rise, set) = For(day.AddDays(shift), location);
            if (rise is { } r && r - offset <= now && (last is null || r - offset > last.Value.At))
                last = (r - offset, true);
            if (set is { } s && s + offset <= now && (last is null || s + offset > last.Value.At))
                last = (s + offset, false);
        }

        if (last is { } found)
            return !found.Rise;
        return !IsPolarDay(day, location);
    }

    /// <summary>
    /// Najblizsia zmena tmy po case <paramref name="now" /> (posunuty vychod alebo zapad) do dvoch dni; null = ziadna
    /// (polarny den alebo noc).
    /// </summary>
    public static DateTimeOffset? NextChange(DateTimeOffset now, GeoLocation location, int offsetMinutes = 0)
    {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var day = DateOnly.FromDateTime(now.UtcDateTime);
        DateTimeOffset? next = null;
        for (var shift = -1; shift <= 2; shift++)
        {
            var (rise, set) = For(day.AddDays(shift), location);
            foreach (var at in new[] { rise - offset, set + offset })
            {
                if (at is { } t && t > now && (next is null || t < next))
                    next = t;
            }
        }

        return next;
    }

    /// <summary>Slnko v ten den nezapada (polarny den); false aj pri polarnej noci.</summary>
    public static bool IsPolarDay(DateOnly date, GeoLocation location) => CosHourAngle(date, location, false) < -1;

    private static DateTimeOffset? Event(DateOnly date, GeoLocation location, bool rise)
    {
        var cosH = CosHourAngle(date, location, rise, out var t, out var ra);
        if (cosH is > 1 or < -1)
            return null;

        var h = rise ? 360 - Degrees(Math.Acos(cosH)) : Degrees(Math.Acos(cosH));
        var local = h / 15 + ra - 0.06571 * t - 6.622;
        var ut = Normalize(local - location.Longitude / 15, 24);
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddHours(ut);
    }

    private static double CosHourAngle(DateOnly date, GeoLocation location, bool rise) => CosHourAngle(date, location, rise, out _, out _);

    private static double CosHourAngle(DateOnly date, GeoLocation location, bool rise, out double t, out double ra)
    {
        var hour = location.Longitude / 15;
        t = date.DayOfYear + ((rise ? 6 : 18) - hour) / 24;

        var m = 0.9856 * t - 3.289;
        var l = Normalize(m + 1.916 * Sin(m) + 0.020 * Sin(2 * m) + 282.634, 360);
        ra = Normalize(Degrees(Math.Atan(0.91764 * Math.Tan(Radians(l)))), 360);
        ra = (ra + (Math.Floor(l / 90) * 90 - Math.Floor(ra / 90) * 90)) / 15;

        var sinDec = 0.39782 * Sin(l);
        var cosDec = Math.Cos(Math.Asin(sinDec));
        return (Math.Cos(Radians(Zenith)) - sinDec * Sin(location.Latitude)) / (cosDec * Math.Cos(Radians(location.Latitude)));
    }

    private static double Sin(double degrees) => Math.Sin(Radians(degrees));

    private static double Radians(double degrees) => degrees * Math.PI / 180;

    private static double Degrees(double radians) => radians * 180 / Math.PI;

    private static double Normalize(double value, double range) => (value % range + range) % range;
}
