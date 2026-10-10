using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Database;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Api.Database;

/// <summary>POST /api/database/test - skuska pripojenia k PostgreSQL podla formulara.</summary>
public sealed class TestDatabase : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("database/test", async (PostgresSettingsRequest request, ICommandHandler<TestDatabaseCommand, DatabaseTestResultDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new TestDatabaseCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<DatabaseTestResultDto>()
            .RequireAuthorization(Permissions.SettingsManage);
}
