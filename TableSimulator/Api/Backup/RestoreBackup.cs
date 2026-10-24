using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Backup;
using TableSimulator.Contracts.Backup;

namespace TableSimulator.Api.Backup;

/// <summary>POST /api/backup/restore - pripravi obnovu zo zalohy (telo = ZIP) na najblizsi start servera.</summary>
public sealed class RestoreBackup : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("backup/restore", async (HttpRequest request, ICommandHandler<RestoreBackupCommand, BackupInfoDto> handler,
                CancellationToken cancellationToken) =>
            {
                // zaloha je vacsia nez predvoleny limit tela poziadavky; vacsiu zastavi az obnova
                if (request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
                    limit.MaxRequestBodySize = BackupErrors.MaxBytes + 1;
                return (await handler.Handle(new RestoreBackupCommand(request.Body), cancellationToken)).Match(Results.Ok, CustomResults.Problem);
            })
            .Accepts<Stream>("application/octet-stream")
            .Produces<BackupInfoDto>()
            .RequireAuthorization(Permissions.ServerManage);
}
