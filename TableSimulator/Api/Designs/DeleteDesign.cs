using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Designs;

namespace TableSimulator.Api.Designs;

/// <summary>DELETE /api/designs/{id} - zmaze vlastny vzhlad (vstavany s rovnakym id sa vrati).</summary>
public sealed class DeleteDesign : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("designs/{id}", async (string id, ICommandHandler<DeleteDesignCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteDesignCommand(id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(204)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
