using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Virtual;

namespace TableSimulator.Api.Virtual;

/// <summary>DELETE /api/virtual/{id} - odstrani virtualnu tabulu.</summary>
public sealed class DeleteVirtualBoard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("virtual/{id:int}", async (int id, ICommandHandler<DeleteVirtualBoardCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteVirtualBoardCommand(id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
