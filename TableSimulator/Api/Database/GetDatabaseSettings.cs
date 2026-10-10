using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Database;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Api.Database;

/// <summary>GET /api/database - pouzivana a vybrana databaza (SQLite alebo PostgreSQL).</summary>
public sealed class GetDatabaseSettings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("database", async (IQueryHandler<GetDatabaseSettingsQuery, DatabaseSettingsDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetDatabaseSettingsQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<DatabaseSettingsDto>()
            .RequireAuthorization(Permissions.SettingsManage);
}
