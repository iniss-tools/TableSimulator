using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.OAuth;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Api.OAuth;

/// <summary>POST /api/oauth/requests/{id} - povoli alebo zamietne pristup klienta MCP; vrati adresu navratu ku klientovi.</summary>
public sealed class DecideOAuthRequest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("oauth/requests/{id}", async (string id, OAuthDecisionRequest request,
                ICommandHandler<DecideOAuthRequestCommand, OAuthRedirectDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DecideOAuthRequestCommand(id, request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<OAuthRedirectDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
