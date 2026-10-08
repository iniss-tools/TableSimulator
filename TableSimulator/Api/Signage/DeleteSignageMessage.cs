using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>DELETE /api/signage/messages/{id} - odstrani spravu, ak sa nikde nepouziva.</summary>
public sealed class DeleteSignageMessage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("signage/messages/{id:int}", async (int id, ICommandHandler<DeleteSignageCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteSignageCommand(SignageEntity.Message, id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
