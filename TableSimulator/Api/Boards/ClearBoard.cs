using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;

namespace TableSimulator.Api.Boards;

/// <summary>POST /api/boards/{id}/clear - zmaze obsah tabule.</summary>
public sealed class ClearBoard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("boards/{id}/clear", async (string id, ICommandHandler<ClearBoardCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new ClearBoardCommand(id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
