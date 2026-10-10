using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Scenarios;

namespace TableSimulator.Api.Scenarios;

/// <summary>POST /api/scenarios/{id}/start - spusti scenar porucha od zaciatku.</summary>
public sealed class StartFaultScenario : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("scenarios/{id:int}/start", async (int id, ICommandHandler<StartFaultScenarioCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new StartFaultScenarioCommand(id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
