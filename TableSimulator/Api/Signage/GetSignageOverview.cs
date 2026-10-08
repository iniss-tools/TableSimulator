using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>GET /api/signage - spravy, playlisty, skupiny a panely s tym, co prave hraju.</summary>
public sealed class GetSignageOverview : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("signage", async (IQueryHandler<GetSignageOverviewQuery, SignageOverviewDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetSignageOverviewQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<SignageOverviewDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
