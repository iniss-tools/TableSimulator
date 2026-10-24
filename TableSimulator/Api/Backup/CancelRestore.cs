using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Backup;

namespace TableSimulator.Api.Backup;

/// <summary>DELETE /api/backup/restore - zrusi pripravenu obnovu.</summary>
public sealed class CancelRestore : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("backup/restore", async (ICommandHandler<CancelRestoreCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new CancelRestoreCommand(), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.ServerManage);
}
