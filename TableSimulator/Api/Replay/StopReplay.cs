using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Replay;

namespace TableSimulator.Api.Replay;

/// <summary>POST /api/replay/stop - zastavi prehravanie.</summary>
public sealed class StopReplay : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("replay/stop", async (ICommandHandler<StopReplayCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new StopReplayCommand(), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .RequireAuthorization(Permissions.RecordingsManage);
}
