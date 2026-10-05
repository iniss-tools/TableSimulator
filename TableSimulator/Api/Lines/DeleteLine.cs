using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Lines;

namespace TableSimulator.Api.Lines;

/// <summary>DELETE /api/lines/{number} - odstrani linku aj s tabulami.</summary>
public sealed class DeleteLine : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("lines/{number:int}", async (int number, ICommandHandler<DeleteLineCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteLineCommand(number), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorEdit);
}
