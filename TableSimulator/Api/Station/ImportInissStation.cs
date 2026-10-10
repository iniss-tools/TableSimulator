using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Api.Station;

/// <summary>POST /api/station/iniss - zalozi linky a tabule instalacie INISSu z nastavenia.</summary>
public sealed class ImportInissStation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("station/iniss", async (ICommandHandler<ImportInissStationCommand, StationImportedDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new ImportInissStationCommand(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<StationImportedDto>()
            .RequireAuthorization(Permissions.LinesManage);
}
