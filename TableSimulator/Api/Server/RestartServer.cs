using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Server;

namespace TableSimulator.Api.Server;

/// <summary>POST /api/server/restart - restart sluzby Windows.</summary>
public sealed class RestartServer : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("server/restart", async (ICommandHandler<RestartServerCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new RestartServerCommand(), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(204)
            .RequireAuthorization(Permissions.ServerManage);
}
