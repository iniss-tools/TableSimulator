using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Frames;

namespace TableSimulator.Api.Frames;

/// <summary>DELETE /api/log - vymaze zaznam komunikacie.</summary>
public sealed class ClearLog : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("log", async (ICommandHandler<ClearLogCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new ClearLogCommand(), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
