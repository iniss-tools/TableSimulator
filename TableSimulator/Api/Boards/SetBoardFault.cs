using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Boards;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Api.Boards;

/// <summary>POST /api/boards/{id}/fault - predstierana porucha tabule.</summary>
public sealed class SetBoardFault : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("boards/{id}/fault", async (string id, BoardFaultDto fault, ICommandHandler<SetBoardFaultCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetBoardFaultCommand(id, fault), cancellationToken)).Match(Results.NoContent, CustomResults.Problem));
}
