using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Database;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Api.Database;

/// <summary>PUT /api/database - zmena vyberu databazy; prejavi sa po restarte servera.</summary>
public sealed class UpdateDatabaseSettings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("database", async (UpdateDatabaseRequest request, ICommandHandler<UpdateDatabaseSettingsCommand, DatabaseSettingsDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateDatabaseSettingsCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<DatabaseSettingsDto>()
            .RequireAuthorization(Permissions.SettingsManage);
}
