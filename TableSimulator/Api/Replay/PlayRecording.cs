using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Replay;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Api.Replay;

/// <summary>POST /api/replay - prehra zaznam.</summary>
public sealed class PlayRecording : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("replay", async (PlayRequest request, ICommandHandler<PlayRecordingCommand, ReplayStatusDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new PlayRecordingCommand(request.Name, request.Speed, request.Loop, request.Position, request.Paused), cancellationToken))
            .Match(Results.Ok, CustomResults.Problem))
            .Produces<ReplayStatusDto>()
            .RequireAuthorization(Permissions.RecordingsManage);
}
