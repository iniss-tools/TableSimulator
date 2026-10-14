namespace TableSimulator.Contracts.OAuth;

/// <summary>Kam prehliadac presmerovat po rozhodnuti - spat ku klientovi s kodom alebo s chybou.</summary>
/// <param name="RedirectUrl">adresa navratu klienta s vysledkom</param>
public sealed record OAuthRedirectDto(string RedirectUrl);
