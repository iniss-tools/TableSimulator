using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Recordings;

namespace TableSimulator.Api.Recordings;

/// <summary>GET /api/recordings/{name}/file - subor zaznamu na stiahnutie.</summary>
public sealed class DownloadRecording : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("recordings/{name}/file", async (string name, IQueryHandler<GetRecordingFileQuery, RecordingFile> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetRecordingFileQuery(name), cancellationToken))
            .Match(file => Results.File(file.Path, "application/x-ndjson", file.DownloadName), CustomResults.Problem))
            .Produces(StatusCodes.Status200OK, contentType: "application/x-ndjson")
            .RequireAuthorization(Permissions.SimulatorView);
}
