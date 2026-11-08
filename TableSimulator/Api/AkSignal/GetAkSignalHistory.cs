using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Devices;
using TableSimulator.Contracts.AkSignal;

namespace TableSimulator.Api.AkSignal;

/// <summary>GET /api/aksignal/history?from=&amp;to=&amp;board= - historia ustredni AK Signal za obdobie (ustredna volitelna).</summary>
public sealed class GetAkSignalHistory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("aksignal/history", async (DateTimeOffset from, DateTimeOffset to, string? board,
            IQueryHandler<GetDeviceHistoryQuery, DeviceHistoryDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetDeviceHistoryQuery(from, to, board), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<DeviceHistoryDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
