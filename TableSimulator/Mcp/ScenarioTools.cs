using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Scenarios;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Mcp;

/// <summary>Nastroje scenarov poruch.</summary>
[McpServerToolType]
public static class ScenarioTools
{
    [McpServerTool(Name = "list_scenarios", Title = "Fault scenarios", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Fault scenarios: timed steps (at [ms], duration [ms]) that simulate a board fault, a board power loss or a line outage, "
                 + "and for a running scenario its start, cycle and active steps. After a power loss the board is empty until INISS sends its content again.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<ScenariosResult> ListScenarios(
        IQueryHandler<GetFaultScenariosQuery, IReadOnlyList<FaultScenarioDto>> handler,
        CancellationToken cancellationToken) =>
        new((await handler.Handle(new GetFaultScenariosQuery(), cancellationToken)).ValueOrThrow());
}

/// <summary>Scenare poruch.</summary>
public sealed record ScenariosResult(IReadOnlyList<FaultScenarioDto> Scenarios);
