using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>GET /api/signage/export - zaloha informacnych panelov (ZIP s definiciami a nahratymi subormi).</summary>
public sealed class ExportSignage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("signage/export", async (IQueryHandler<ExportSignageQuery, SignageExportFile> handler, CancellationToken cancellationToken) =>
                (await handler.Handle(new ExportSignageQuery(), cancellationToken)).Match(
                    // docasny subor sa zmaze po odoslani
                    file => Results.File(new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.None, 81920, FileOptions.DeleteOnClose),
                        "application/zip", file.FileName),
                    CustomResults.Problem))
            .Produces(StatusCodes.Status200OK, contentType: "application/zip")
            .RequireAuthorization(Permissions.SimulatorView);
}
