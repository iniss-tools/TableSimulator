namespace TableSimulator.Contracts.AkSignal;

/// <summary>
/// Historia ustredni AK Signal za obdobie: zmeny stavu svetla (aj posledna pred obdobim - stav na jeho zaciatku),
/// hlasenia, ktore v obdobi hrali, a log svetiel servera INISS2, ak je server v nastaveni.
/// </summary>
/// <param name="From">zaciatok obdobia</param>
/// <param name="To">koniec obdobia</param>
/// <param name="Lights">zmeny stavu svetla v poradi casu</param>
/// <param name="Announcements">hlasenia v poradi zaciatku</param>
/// <param name="Server">zaznamy logu svetiel servera v poradi casu</param>
public sealed record DeviceHistoryDto(
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<LightStateDto> Lights,
    IReadOnlyList<AnnouncementHistoryDto> Announcements,
    IReadOnlyList<ServerLightEventDto> Server);
