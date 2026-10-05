using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Fonts;

namespace TableSimulator.Api.Fonts;

/// <summary>GET /api/fonts - subory pisiem ELEN.</summary>
public sealed class GetFonts : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("fonts", async (IQueryHandler<GetFontsQuery, string[]> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetFontsQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorView);
}
