using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>PUT /api/signage/panels/{id} - zmeni panel.</summary>
public sealed class UpdateSignagePanel : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("signage/panels/{id:int}", async (int id, SetSignagePanelRequest request, ICommandHandler<SaveSignagePanelCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignagePanelCommand(id, request), cancellationToken)).Match(_ => Results.NoContent(), CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
