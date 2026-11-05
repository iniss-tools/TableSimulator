namespace TableSimulator.Contracts.Station;

/// <summary>Presmerovanie liniek servera INISS2 na simulator (POST /api/station/iniss/redirect).</summary>
/// <param name="Redirect">presmerovat linky; inak vratit povodne adresy</param>
public sealed record InissRedirectRequest(bool Redirect);
