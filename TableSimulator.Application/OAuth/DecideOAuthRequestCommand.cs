using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Application.OAuth;

/// <summary>Rozhodnutie pouzivatela o ziadosti klienta MCP - vrati adresu navratu ku klientovi (s kodom alebo chybou).</summary>
public sealed record DecideOAuthRequestCommand(string Id, OAuthDecisionRequest Request) : ICommand<OAuthRedirectDto>, IAuditable
{
    string IAuditable.AuditTarget => Request.Allow ? "allow " + Request.Role : "deny";
}
