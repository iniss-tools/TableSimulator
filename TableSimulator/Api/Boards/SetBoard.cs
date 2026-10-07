using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Api.Boards;

/// <summary>PUT /api/boards/{line}/{address} - prida alebo zmeni tabulu.</summary>
public sealed class SetBoard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("boards/{line:int}/{address:int}", async (int line, int address, SetBoardRequest request, ICommandHandler<SetBoardCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetBoardCommand(line, address, request), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
