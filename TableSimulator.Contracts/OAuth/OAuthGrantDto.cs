namespace TableSimulator.Contracts.OAuth;

/// <summary>Prepojena aplikacia - povolenie klienta MCP (GET /api/oauth/grants).</summary>
/// <param name="Id">identifikator</param>
/// <param name="ClientName">nazov klienta</param>
/// <param name="UserName">pouzivatel, ktory pristup povolil</param>
/// <param name="Role">rola, s ktorou klient pristupuje</param>
/// <param name="RoleName">nazov vlastnej roly; prazdny pri vstavanej (web ju prelozi)</param>
/// <param name="CreatedAt">kedy bol pristup povoleny</param>
/// <param name="LastUsedAt">posledne pouzitie (s presnostou na minutu); null = este nikdy</param>
public sealed record OAuthGrantDto(Guid Id, string ClientName, string UserName, string Role, string RoleName, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt);
