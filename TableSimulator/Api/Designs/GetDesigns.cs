using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Designs;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Api.Designs;

/// <summary>GET /api/designs - vzhlady tabul.</summary>
[UsedImplicitly]
public sealed class GetDesigns : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("designs", async (IQueryHandler<GetDesignsQuery, DesignInfoDto[]> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetDesignsQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorView);
}
