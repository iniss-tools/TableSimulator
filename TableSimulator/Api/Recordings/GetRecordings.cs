using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Recordings;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Api.Recordings;

/// <summary>GET /api/recordings - zaznamy so stavom zaznamu a prehravania.</summary>
public sealed class GetRecordings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("recordings", async (IQueryHandler<GetRecordingsQuery, RecordingsDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetRecordingsQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<RecordingsDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
