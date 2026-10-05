using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Api.Virtual;

/// <summary>GET /api/virtual - virtualne tabule, najdene XML tabul a sledovane priecinky.</summary>
public sealed class GetVirtualOverview : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("virtual", async (IQueryHandler<GetVirtualOverviewQuery, VirtualOverviewDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetVirtualOverviewQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorView);
}
