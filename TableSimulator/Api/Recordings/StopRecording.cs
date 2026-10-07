using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Recordings;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Api.Recordings;

/// <summary>POST /api/recordings/stop - ukonci zaznam.</summary>
public sealed class StopRecording : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("recordings/stop", async (ICommandHandler<StopRecordingCommand, RecorderStatusDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new StopRecordingCommand(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<RecorderStatusDto>()
            .RequireAuthorization(Permissions.RecordingsManage);
}
