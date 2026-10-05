using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Api.Settings;

/// <summary>GET /api/settings - nastavenie servera.</summary>
public sealed class GetSettings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("settings", async (IQueryHandler<GetSettingsQuery, SettingsDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetSettingsQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem));
}
