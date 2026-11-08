namespace TableSimulator.Contracts.Display;

/// <summary>Hlasenie, ktore zariadenie simulatora prehralo alebo na ktore zopol rozhlas.</summary>
/// <param name="Id">poradove cislo hlasenia v zariadeni</param>
/// <param name="Channel">kanal ustredne (1, 2), pri AKSIGNAL 0</param>
/// <param name="Sounds">zvuky, ako ich poslal INISS; prazdne = zvuk hra server sam</param>
/// <param name="StartedAt">zaciatok</param>
/// <param name="Result">ako skoncilo</param>
public sealed record AnnouncementDto(long Id, int Channel, IReadOnlyList<string> Sounds, DateTimeOffset StartedAt, AnnouncementResult Result)
{
    /// <summary>Titulok hlasenia; null = ziadny.</summary>
    public string? Title { get; init; }

    /// <summary>Ako dlho ma hrat [ms]; null = kym ho INISS nezastavi.</summary>
    public long? DurationMs { get; init; }

    /// <summary>Hodnota zapnutia rozhlasu AKSIGNAL; null = ina ustredna.</summary>
    public int? Value { get; init; }

    /// <summary>Koniec; null = este hra.</summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>Fronta vzdialeneho INISSu ("" = jedina fronta); null = ustredna.</summary>
    public string? Queue { get; init; }

    /// <summary>Okruh hlasenia vzdialeneho INISSu; null = nezadany.</summary>
    public string? Circuit { get; init; }
}
