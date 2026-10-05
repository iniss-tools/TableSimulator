using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Lines;
using TableSimulator.Contracts.Lines;

namespace TableSimulator.Api.Lines;

/// <summary>PUT /api/lines/{number} - prida alebo zmeni linku.</summary>
public sealed class SetLine : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("lines/{number:int}", async (int number, SetLineRequest request, ICommandHandler<SetLineCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetLineCommand(number, request), cancellationToken)).Match(Results.NoContent, CustomResults.Problem));
}
