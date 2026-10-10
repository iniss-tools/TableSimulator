using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>PUT /api/signage/messages/{id} - zmeni spravu.</summary>
public sealed class UpdateSignageMessage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("signage/messages/{id:int}", async (int id, SetSignageMessageRequest request, ICommandHandler<SaveSignageMessageCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignageMessageCommand(id, request), cancellationToken)).Match(_ => Results.NoContent(), CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SignageManage);
}
