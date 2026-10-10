using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Scenarios;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Api.Scenarios;

/// <summary>GET /api/scenarios - scenare porucha s behom.</summary>
public sealed class GetFaultScenarios : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("scenarios", async (IQueryHandler<GetFaultScenariosQuery, IReadOnlyList<FaultScenarioDto>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetFaultScenariosQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<IReadOnlyList<FaultScenarioDto>>()
            .RequireAuthorization(Permissions.SimulatorView);
}
