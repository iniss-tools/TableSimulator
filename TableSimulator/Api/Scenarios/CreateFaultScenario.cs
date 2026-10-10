using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Scenarios;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Api.Scenarios;

/// <summary>POST /api/scenarios - novy scenar porucha.</summary>
public sealed class CreateFaultScenario : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("scenarios", async (FaultScenarioRequest request, ICommandHandler<CreateFaultScenarioCommand, FaultScenarioDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new CreateFaultScenarioCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<FaultScenarioDto>()
            .RequireAuthorization(Permissions.ScenariosManage);
}
