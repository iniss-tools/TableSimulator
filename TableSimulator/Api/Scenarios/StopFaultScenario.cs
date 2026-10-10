using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Scenarios;

namespace TableSimulator.Api.Scenarios;

/// <summary>POST /api/scenarios/{id}/stop - zastavi scenar porucha a vrati, co nastavil.</summary>
public sealed class StopFaultScenario : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("scenarios/{id:int}/stop", async (int id, ICommandHandler<StopFaultScenarioCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new StopFaultScenarioCommand(id), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.ScenariosManage);
}
