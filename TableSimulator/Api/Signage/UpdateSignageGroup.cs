using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>PUT /api/signage/groups/{id} - zmeni skupinu panelov.</summary>
public sealed class UpdateSignageGroup : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("signage/groups/{id:int}", async (int id, SetSignageGroupRequest request, ICommandHandler<SaveSignageGroupCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignageGroupCommand(id, request), cancellationToken)).Match(_ => Results.NoContent(), CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
