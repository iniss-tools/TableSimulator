namespace TableSimulator.Contracts.OAuth;

/// <summary>Rozhodnutie o ziadosti klienta MCP (POST /api/oauth/requests/{id}).</summary>
/// <param name="Allow">povolit pristup; false = zamietnut</param>
/// <param name="Role">rola klienta (najviac rola pouzivatela); null = rola pouzivatela</param>
public sealed record OAuthDecisionRequest(bool Allow, string? Role);
