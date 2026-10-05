using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Monitor;
using TableSimulator.Contracts.Monitor;

namespace TableSimulator.Api.Monitor;

/// <summary>GET /api/monitor - instalacia ELEN Monitora.</summary>
public sealed class GetMonitor : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("monitor", async (IQueryHandler<GetMonitorQuery, MonitorDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetMonitorQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorView);
}
