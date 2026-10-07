using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Archive;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Archive;

namespace TableSimulator.Api.Archive;

/// <summary>GET /api/archive - dni dennych archivov tabul a hlaseni.</summary>
public sealed class GetArchiveDays : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("archive", async (IQueryHandler<GetArchiveDaysQuery, ArchiveDaysDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetArchiveDaysQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<ArchiveDaysDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
