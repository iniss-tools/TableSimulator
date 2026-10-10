using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Scenarios;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Api.Scenarios;

/// <summary>PUT /api/scenarios/{id} - zmeni scenar porucha (beziaci pokracuje s novymi krokmi).</summary>
public sealed class SetFaultScenario : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("scenarios/{id:int}", async (int id, FaultScenarioRequest request, ICommandHandler<SetFaultScenarioCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetFaultScenarioCommand(id, request), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
