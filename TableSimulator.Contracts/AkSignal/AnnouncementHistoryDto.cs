using TableSimulator.Contracts.Display;

namespace TableSimulator.Contracts.AkSignal;

/// <summary>Skoncene hlasenie ustredne AK Signal alebo rozhlasu vzdialeneho INISSu.</summary>
/// <param name="BoardId">zariadenie <c>linka-adresa</c></param>
/// <param name="Channel">kanal (1, 2), pri AKSIGNAL a vzdialenom INISSe 0</param>
/// <param name="Sounds">zvuky; prazdne = rozhlas AKSIGNAL (zvuk hral server)</param>
/// <param name="StartedAt">zaciatok</param>
/// <param name="EndedAt">koniec</param>
/// <param name="Result">ako skoncilo</param>
public sealed record AnnouncementHistoryDto(
    string BoardId,
    int Channel,
    IReadOnlyList<string> Sounds,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    AnnouncementResult Result)
{
    /// <summary>Hodnota zapnutia rozhlasu AKSIGNAL; null = ina ustredna.</summary>
    public int? Value { get; init; }

    /// <summary>Titulok hlasenia; null = ziadny.</summary>
    public string? Title { get; init; }

    /// <summary>Fronta vzdialeneho INISSu ("" = jedina fronta); null = ustredna.</summary>
    public string? Queue { get; init; }

    /// <summary>Okruh hlasenia vzdialeneho INISSu; null = nezadany.</summary>
    public string? Circuit { get; init; }
}
