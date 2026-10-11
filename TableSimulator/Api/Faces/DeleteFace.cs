using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Faces;

namespace TableSimulator.Api.Faces;

/// <summary>DELETE /api/faces/{id} - odstrani vlastnu predlohu cela, ktoru ziadna tabula nepouziva.</summary>
[UsedImplicitly]
public sealed class DeleteFace : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("faces/{id}", async (string id, ICommandHandler<DeleteFaceCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteFaceCommand(id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.DesignsManage);
}
