using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>DELETE /api/signage/panels/{id} - odstrani panel, ak sa nikde nepouziva.</summary>
public sealed class DeleteSignagePanel : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("signage/panels/{id:int}", async (int id, ICommandHandler<DeleteSignageCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteSignageCommand(SignageEntity.Panel, id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SignageManage);
}
