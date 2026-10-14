namespace TableSimulator.Contracts.OAuth;

/// <summary>Ziadost klienta MCP o pristup na stranke povolenia (GET /api/oauth/requests/{id}).</summary>
/// <param name="Id">identifikator ziadosti</param>
/// <param name="ClientName">nazov, ktory klient poslal pri registracii (napr. Visual Studio Code)</param>
/// <param name="RedirectHost">kam sa po rozhodnuti prehliadac vrati (schema a server adresy navratu)</param>
/// <param name="Roles">roly, ktore moze prihlaseny pouzivatel klientovi dat</param>
/// <param name="Role">predvolena rola - rola pouzivatela</param>
public sealed record OAuthRequestDto(string Id, string ClientName, string RedirectHost, IReadOnlyList<OAuthRoleDto> Roles, string Role);
