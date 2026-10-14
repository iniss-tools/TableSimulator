using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Application.OAuth;

/// <summary>Ziadost klienta MCP pre stranku povolenia a roly, ktore moze prihlaseny pouzivatel dat.</summary>
public sealed record GetOAuthRequestQuery(string Id) : IQuery<OAuthRequestDto>;
