using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Api.Station;

/// <summary>POST /api/station - linky a tabule stanice INISSu (GVDEditor).</summary>
public sealed class ImportStation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("station", async (StationImportRequest request, ICommandHandler<ImportStationCommand, StationImportedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new ImportStationCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<StationImportedDto>()
            .RequireAuthorization(Permissions.LinesManage);
}
