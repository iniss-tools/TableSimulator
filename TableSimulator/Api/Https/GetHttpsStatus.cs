using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Https;
using TableSimulator.Contracts.Https;

namespace TableSimulator.Api.Https;

/// <summary>GET /api/https - stav HTTPS a certifikatu servera.</summary>
public sealed class GetHttpsStatus : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("https", async (IQueryHandler<GetHttpsStatusQuery, HttpsStatusDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetHttpsStatusQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .RequireAuthorization(Permissions.SettingsManage);
}
