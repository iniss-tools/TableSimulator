namespace TableSimulator.Core.Boards.Announcements;

/// <summary>
/// Hlasenie, ktore zariadenie simulatora prehralo alebo na ktore zopol rozhlas - spolocne pre ustrednu AK Signal
/// a vzdialeny INISS.
/// </summary>
/// <param name="Id">poradove cislo hlasenia v zariadeni</param>
/// <param name="Channel">kanal ustredne (1, 2), pri AKSIGNAL a vzdialenom INISSe 0</param>
/// <param name="Sounds">zvuky, ako ich poslal INISS (AKSIGNALUH, vzdialeny INISS); prazdne = zvuk hra server sam</param>
/// <param name="StartedAt">zaciatok</param>
public sealed record Announcement(long Id, int Channel, IReadOnlyList<string> Sounds, DateTimeOffset StartedAt)
{
    /// <summary>Ticho pred a po kazdej nahravke v poradi <see cref="Sounds" />; null = nahravky idu hned po sebe.</summary>
    public IReadOnlyList<SoundPause>? Pauses { get; init; }

    /// <summary>Titulok hlasenia, ak ho INISS posiela; null = ziadny.</summary>
    public string? Title { get; init; }

    /// <summary>Ako dlho ma hrat; null = kym ho INISS nezastavi.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Hodnota prikazu zapnutia rozhlasu AKSIGNAL (<c>daymode</c>/<c>nightmode</c>); null = ina ustredna.</summary>
    public int? Value { get; init; }

    /// <summary>Fronta vzdialeneho INISSu ("" = jedina fronta); null = ine zariadenie.</summary>
    public string? Queue { get; init; }

    /// <summary>Okruh hlasenia vzdialeneho INISSu (<c>LINE</c>, ak frontu zdiela viac okruhov); null = nezadany.</summary>
    public string? Circuit { get; init; }

    /// <summary>Koniec; null = este hra.</summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>Ako skoncilo.</summary>
    public AnnouncementResult Result { get; init; } = AnnouncementResult.Playing;

    /// <summary>Planovany koniec; null = bez dlzky.</summary>
    public DateTimeOffset? PlannedEnd => Duration is { } duration ? StartedAt + duration : null;

    /// <summary>Hlasenie ukoncene v case <paramref name="at" />.</summary>
    public Announcement End(DateTimeOffset at, AnnouncementResult result) => this with { EndedAt = at, Result = result };
}
