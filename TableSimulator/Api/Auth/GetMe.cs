using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Api.Auth;

/// <summary>GET /api/auth/me - prihlaseny pouzivatel a rezim prihlasenia (aj bez prihlasenia).</summary>
public sealed class GetMe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("auth/me", async (IQueryHandler<GetMeQuery, MeDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetMeQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .AllowAnonymous();
}
