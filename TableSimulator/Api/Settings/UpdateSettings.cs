using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Api.Settings;

/// <summary>PUT /api/settings - ulozi nastavenie servera.</summary>
public sealed class UpdateSettings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("settings", async (UpdateSettingsRequest request, ICommandHandler<UpdateSettingsCommand, SettingsUpdatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateSettingsCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<SettingsUpdatedDto>()
            .RequireAuthorization(Permissions.SettingsManage);
}
