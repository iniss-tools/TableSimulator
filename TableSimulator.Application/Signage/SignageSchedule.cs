using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Co skupina hra: playlist s casom, od ktoreho sa jeho spravy striedaju, predvolena sprava alebo nic.</summary>
/// <param name="Playlist">platny playlist (aj predvoleny); null = nie</param>
/// <param name="Anchor">od kedy sa spravy playlistu striedaju</param>
/// <param name="Message">predvolena sprava, ked nehra ziadny playlist</param>
public readonly record struct SignageChoice(SignagePlaylistDto? Playlist, DateTime Anchor, int? Message);

/// <summary>
/// Rozvrh playlistov ako v INISSView 2 - vsetko podla miestneho casu servera, bez stavu panelov (vsetky panely skupiny
/// hraju v tej istej chvili to iste). Okno playlistu patri dnu, v ktorom zacina (okno cez polnoc pokracuje do rana
/// nasledujuceho dna); rovnaky zaciatok a koniec = cely den.
/// </summary>
public static class SignageSchedule
{
    private const int MinutesPerDay = 24 * 60;

    /// <summary>
    /// Vyber v case <paramref name="local" />: z platnych playlistov vyhra jeden den pred dnami v tyzdni a tie pred
    /// kazdym dnom, potom neskor zacate okno a nizsie cislo. Bez platneho playlistu hra predvoleny playlist od konca
    /// posledneho okna dnes (inak od polnoci), bez neho predvolena sprava.
    /// </summary>
    public static SignageChoice Select(IReadOnlyList<SignagePlaylistDto> playlists, SignagePlaylistDto? fallback, int? message, DateTime local)
    {
        ArgumentNullException.ThrowIfNull(playlists);
        SignagePlaylistDto? best = null;
        var bestStart = DateTime.MinValue;
        foreach (var playlist in playlists)
        {
            if (Occurrence(playlist.Definition, local) is not { } window)
                continue;
            if (best is null || Better(playlist, window.Start, best, bestStart))
            {
                best = playlist;
                bestStart = window.Start;
            }
        }

        if (best is not null)
            return new SignageChoice(best, bestStart, null);

        if (fallback is not null)
        {
            // od konca posledneho dnes skonceneho okna (aj vcerajsieho cez polnoc) - po skonceni playlistu zacne
            // predvoleny od zaciatku
            var anchor = local.Date;
            var today = DateOnly.FromDateTime(local);
            foreach (var playlist in playlists)
            foreach (var window in Windows(playlist.Definition, today.AddDays(-1)).Concat(Windows(playlist.Definition, today)))
            {
                if (window.End <= local && window.End > anchor)
                    anchor = window.End;
            }

            return new SignageChoice(fallback, anchor, null);
        }

        return new SignageChoice(null, local.Date, message);
    }

    /// <summary>Najblizsia cela minuta po <paramref name="local" />, v ktorej sa vyber zmeni (najviac o den).</summary>
    public static DateTime NextChange(IReadOnlyList<SignagePlaylistDto> playlists, SignagePlaylistDto? fallback, int? message, DateTime local)
    {
        var current = Select(playlists, fallback, message, local);
        var minute = new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, local.Kind);
        for (var i = 1; i <= MinutesPerDay; i++)
        {
            var at = minute.AddMinutes(i);
            var next = Select(playlists, fallback, message, at);
            // kotva sa rata len pri playliste - predvolena sprava alebo nic sa o polnoci nemeni
            if (next.Playlist?.Id != current.Playlist?.Id || next.Message != current.Message || next.Playlist is not null && next.Anchor != current.Anchor)
                return at;
        }

        return minute.AddMinutes(MinutesPerDay);
    }

    /// <summary>Poradie polozky (s trvaniami <paramref name="durations" /> v sekundach), ktora hra v case <paramref name="local" />.</summary>
    public static int SlotAt(IReadOnlyList<int> durations, DateTime anchor, DateTime local)
    {
        ArgumentNullException.ThrowIfNull(durations);
        var total = durations.Sum(d => (long)Math.Max(1, d));
        if (durations.Count == 0)
            return 0;
        var elapsed = (long)Math.Floor((local - anchor).TotalSeconds) % total;
        if (elapsed < 0)
            elapsed += total;
        for (var i = 0; i < durations.Count; i++)
        {
            elapsed -= Math.Max(1, durations[i]);
            if (elapsed < 0)
                return i;
        }

        return durations.Count - 1;
    }

    /// <summary>Okno playlistu, ktore obsahuje cas <paramref name="local" />; null = playlist teraz neplati.</summary>
    public static (DateTime Start, DateTime End)? Occurrence(SetSignagePlaylistRequest playlist, DateTime local)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        var today = DateOnly.FromDateTime(local);
        foreach (var window in Windows(playlist, today.AddDays(-1)).Concat(Windows(playlist, today)))
        {
            if (window.Start <= local && local < window.End)
                return window;
        }

        return null;
    }

    /// <summary>Okno playlistu zacinajuce v den <paramref name="day" /> (ak v ten den plati).</summary>
    private static IEnumerable<(DateTime Start, DateTime End)> Windows(SetSignagePlaylistRequest playlist, DateOnly day)
    {
        if (!Valid(playlist, day))
            yield break;
        var from = Math.Clamp(playlist.From, 0, MinutesPerDay - 1);
        var to = Math.Clamp(playlist.To, 0, MinutesPerDay);
        var length = to > from ? to - from : to < from ? MinutesPerDay - from + to : MinutesPerDay;
        var start = day.ToDateTime(TimeOnly.MinValue).AddMinutes(from);
        yield return (start, start.AddMinutes(length));
    }

    /// <summary>Plati playlist v den <paramref name="day" />.</summary>
    public static bool Valid(SetSignagePlaylistRequest playlist, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        return playlist.Period switch
        {
            SignagePeriod.Once => day == playlist.Start,
            SignagePeriod.Daily => InRange(playlist, day),
            SignagePeriod.Weekly => InRange(playlist, day) && playlist.Weekdays.Contains(IsoDay(day)),
            _ => false
        };
    }

    /// <summary>Den v tyzdni 1 (pondelok) az 7 (nedela).</summary>
    public static int IsoDay(DateOnly day) => ((int)day.DayOfWeek + 6) % 7 + 1;

    private static bool InRange(SetSignagePlaylistRequest playlist, DateOnly day) => day >= playlist.Start && (playlist.End is not { } end || day <= end);

    private static int Priority(SignagePeriod period) => period switch
    {
        SignagePeriod.Once => 0,
        SignagePeriod.Weekly => 1,
        _ => 2
    };

    private static bool Better(SignagePlaylistDto candidate, DateTime start, SignagePlaylistDto best, DateTime bestStart)
    {
        var a = Priority(candidate.Definition.Period);
        var b = Priority(best.Definition.Period);
        if (a != b)
            return a < b;
        if (start != bestStart)
            return start > bestStart;
        return candidate.Id < best.Id;
    }
}
