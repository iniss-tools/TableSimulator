namespace TableSimulator.Application.OAuth;

/// <summary>Klient MCP zaregistrovany cez dynamicku registraciu (VS Code, Claude Code) - verejny, bez tajnej hodnoty.</summary>
/// <param name="Id">identifikator klienta (client_id)</param>
/// <param name="Name">nazov, ktory klient poslal</param>
/// <param name="RedirectUris">povolene adresy navratu</param>
/// <param name="CreatedAt">registracia (UTC)</param>
public sealed record OAuthClient(string Id, string Name, string[] RedirectUris, DateTime CreatedAt);
