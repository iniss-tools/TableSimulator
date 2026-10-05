using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Recordings;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Api.Recordings;

/// <summary>POST /api/recordings/start - zacne zaznam.</summary>
public sealed class StartRecording : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("recordings/start", async (StartRecordingRequest request, ICommandHandler<StartRecordingCommand, RecorderStatusDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new StartRecordingCommand(request.Note), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .RequireAuthorization(Permissions.RecordingsManage);
}
