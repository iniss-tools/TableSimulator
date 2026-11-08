using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Api.Boards;

/// <summary>PUT /api/boards/{id}/aksignal - snimac tmy, stavy a casovanie ustredne AK Signal.</summary>
public sealed class SetBoardAkSignal : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("boards/{id}/aksignal", async (string id, AkSignalSettingsRequest settings, ICommandHandler<SetAkSignalCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetAkSignalCommand(id, settings), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.BoardsControl);
}
