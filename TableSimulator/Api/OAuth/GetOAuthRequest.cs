using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.OAuth;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Api.OAuth;

/// <summary>GET /api/oauth/requests/{id} - ziadost klienta MCP pre stranku povolenia.</summary>
public sealed class GetOAuthRequest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("oauth/requests/{id}", async (string id, IQueryHandler<GetOAuthRequestQuery, OAuthRequestDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetOAuthRequestQuery(id), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<OAuthRequestDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
