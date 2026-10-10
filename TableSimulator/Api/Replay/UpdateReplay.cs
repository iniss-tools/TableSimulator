using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Replay;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Api.Replay;

/// <summary>PUT /api/replay - pozastavi, posunie, zmeni rychlost alebo opakovanie posledneho prehravania.</summary>
public sealed class UpdateReplay : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("replay", async (UpdateReplayRequest request, ICommandHandler<UpdateReplayCommand, ReplayStatusDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateReplayCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<ReplayStatusDto>()
            .RequireAuthorization(Permissions.RecordingsManage);
}
