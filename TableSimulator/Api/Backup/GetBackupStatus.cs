using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Backup;
using TableSimulator.Contracts.Backup;

namespace TableSimulator.Api.Backup;

/// <summary>GET /api/backup - pripravena obnova a ci server bezi ako sluzba (restart z webu).</summary>
public sealed class GetBackupStatus : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("backup", async (IQueryHandler<GetBackupStatusQuery, BackupStatusDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetBackupStatusQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<BackupStatusDto>()
            .RequireAuthorization(Permissions.ServerManage);
}
