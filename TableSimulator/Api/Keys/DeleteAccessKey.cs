using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Keys;

namespace TableSimulator.Api.Keys;

/// <summary>DELETE /api/keys/{id} - odstrani pristupovy kluc.</summary>
public sealed class DeleteAccessKey : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("keys/{id:guid}", async (Guid id, ICommandHandler<DeleteAccessKeyCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteAccessKeyCommand(id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .RequireAuthorization(Permissions.UsersManage);
}
