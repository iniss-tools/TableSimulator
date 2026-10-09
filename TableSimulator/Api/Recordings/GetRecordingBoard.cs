using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Recordings;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Api.Recordings;

/// <summary>GET /api/recordings/{name}/boards/{board}?time= - tabula zo zaznamu v case (s od zaciatku).</summary>
public sealed class GetRecordingBoard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("recordings/{name}/boards/{board}", async (string name, string board, double time,
                IQueryHandler<GetRecordingBoardQuery, BoardDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetRecordingBoardQuery(name, board, time), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<BoardDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
