using TableSimulator.Core.Boards;

namespace TableSimulator.Application.Devices;

/// <summary>Zmena stavu svetla ustredne AK Signal v databaze (cas v ms od 1. 1. 1970 UTC, stav ako bity <see cref="LightFlags" />).</summary>
public sealed record LightStateRecord(long Id, int Line, int Address, long TimeMs, int Flags);

/// <summary>
/// Skoncene hlasenie ustredne AK Signal alebo vzdialeneho INISSu v databaze; zvuky oddelene <c>*</c> ako ich posiela
/// server, vysledok klucom (<c>completed</c>, <c>stopped</c>, <c>cancelled</c>); fronta a okruh len pri vzdialenom INISSe.
/// </summary>
public sealed record AnnouncementRecord(
    long Id,
    int Line,
    int Address,
    int Channel,
    long StartMs,
    long EndMs,
    string Result,
    string Sounds,
    int? Value,
    string? Title,
    string? Queue = null,
    string? Circuit = null);

/// <summary>Co dotaz na historiu vrati z databazy.</summary>
/// <param name="Lights">zmeny stavu svetla v obdobi a posledna pred nim pri kazdej ustredni</param>
/// <param name="Announcements">hlasenia, ktore v obdobi hrali</param>
public sealed record DeviceHistoryData(IReadOnlyList<LightStateRecord> Lights, IReadOnlyList<AnnouncementRecord> Announcements);

/// <summary>Historia ustredni AK Signal v databaze.</summary>
public interface IDeviceHistoryRepository
{
    /// <summary>Zapise odobrate zmeny.</summary>
    Task AddAsync(DeviceHistorySample sample, CancellationToken cancellationToken);

    /// <summary>
    /// Historia v obdobi [<paramref name="fromMs" />, <paramref name="toMs" />) aj so stavom na jeho zaciatku; linka a adresa
    /// null = vsetky ustredne.
    /// </summary>
    Task<DeviceHistoryData> QueryAsync(long fromMs, long toMs, int? line, int? address, CancellationToken cancellationToken);

    /// <summary>Odstrani historiu starsiu nez <paramref name="timeMs" /> (posledny stav kazdej ustredne ostava); vrati pocet zaznamov.</summary>
    Task<int> DeleteOlderThanAsync(long timeMs, CancellationToken cancellationToken);
}

/// <summary>Uchovavanie historie ustredni.</summary>
public static class DeviceHistoryRetention
{
    /// <summary>Ako dlho sa historia uchovava [dni].</summary>
    public const int Days = 30;

    /// <summary>Najdlhsie obdobie jedneho dotazu [dni].</summary>
    public const int MaxSpanDays = 7;

    /// <summary>Najviac zaznamov logu servera v jednej odpovedi.</summary>
    public const int MaxServerEvents = 5000;

    /// <summary>Najdlhsi zapisany zoznam zvukov hlasenia (znaky).</summary>
    public const int MaxSounds = 4000;

    /// <summary>Najdlhsi zapisany titulok hlasenia (znaky).</summary>
    public const int MaxTitle = 400;

    /// <summary>Najdlhsie meno fronty a okruhu vzdialeneho INISSu v databaze.</summary>
    public const int MaxName = 100;
}
