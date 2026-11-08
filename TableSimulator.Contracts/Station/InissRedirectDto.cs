namespace TableSimulator.Contracts.Station;

/// <summary>Stav presmerovania liniek servera INISS2 z nastavenia (GET /api/station/iniss/redirect).</summary>
/// <param name="Available">program v nastaveni je server INISS2 a ma linku, ktoru sa da presmerovat</param>
/// <param name="Redirected">aspon jedna linka je presmerovana na simulator</param>
public sealed record InissRedirectDto(bool Available, bool Redirected);
