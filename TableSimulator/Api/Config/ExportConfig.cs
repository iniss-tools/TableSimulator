using System.Text;
using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Config;

namespace TableSimulator.Api.Config;

/// <summary>GET /api/config - linky a tabule ako subor JSON na stiahnutie.</summary>
public sealed class ExportConfig : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("config", async (IQueryHandler<ExportConfigQuery, ConfigFile> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new ExportConfigQuery(), cancellationToken))
            .Match(file => Results.File(Encoding.UTF8.GetBytes(file.Content), "application/json", file.FileName), CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorView);
}
