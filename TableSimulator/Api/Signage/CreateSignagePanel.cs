using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>POST /api/signage/panels - zalozi panel; vrati cislo.</summary>
public sealed class CreateSignagePanel : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("signage/panels", async (SetSignagePanelRequest request, ICommandHandler<SaveSignagePanelCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignagePanelCommand(null, request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<SignageCreatedDto>()
            .RequireAuthorization(Permissions.SignageManage);
}
