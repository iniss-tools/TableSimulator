using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Server;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Api.Server;

/// <summary>GET /api/server - ako server bezi.</summary>
public sealed class GetServerInfo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("server", async (IQueryHandler<GetServerInfoQuery, ServerInfoDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetServerInfoQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<ServerInfoDto>()
            .RequireAuthorization(Permissions.SettingsManage);
}
