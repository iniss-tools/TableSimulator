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

    [McpServerTool(Name = "save_scenario", Title = "Save fault scenario", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Creates a fault scenario (without id) or replaces an existing one. Each step starts atMs after the start of the cycle and lasts durationMs: "
                 + "fault = the board (line + address) simulates the fault, powerLoss = the board is dark and does not answer, lineOutage = the whole line "
                 + "(address null) is disconnected. Steps may overlap. A one-step scenario is the way to switch a board off or cut a line for a while.")]
    [Authorize(Policy = Permissions.ScenariosManage)]
    public static async Task<FaultScenarioDto> SaveScenario(
        IQueryHandler<GetFaultScenariosQuery, IReadOnlyList<FaultScenarioDto>> scenarios,
        ICommandHandler<CreateFaultScenarioCommand, FaultScenarioDto> create,
        ICommandHandler<SetFaultScenarioCommand> update,
        [Description("Name of the scenario.")] string name,
        [Description("Steps of the scenario.")] IReadOnlyList<ScenarioStepDto> steps,
        [Description("Id of the scenario to replace; null = new scenario.")] int? id = null,
        [Description("Repeat the steps in a loop.")] bool loop = false,
        [Description("Length of one cycle [ms]; null = until the end of the last step.")] int? cycleMs = null,
        CancellationToken cancellationToken = default)
    {
        var request = new FaultScenarioRequest(name, loop, cycleMs, steps);
        if (id is null)
            return (await create.Handle(new CreateFaultScenarioCommand(request), cancellationToken)).ValueOrThrow();
        (await update.Handle(new SetFaultScenarioCommand(id.Value, request), cancellationToken)).ThrowIfFailed();
        return await Find(scenarios, id.Value, cancellationToken);
    }

    [McpServerTool(Name = "start_scenario", Title = "Start fault scenario", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Starts a fault scenario from the beginning of its cycle. More scenarios may run at once; their effects are combined.")]
    [Authorize(Policy = Permissions.ScenariosManage)]
    public static async Task<FaultScenarioDto> StartScenario(
        IQueryHandler<GetFaultScenariosQuery, IReadOnlyList<FaultScenarioDto>> scenarios,
        ICommandHandler<StartFaultScenarioCommand> handler,
        [Description("Id of the scenario.")] int id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new StartFaultScenarioCommand(id), cancellationToken)).ThrowIfFailed();
        return await Find(scenarios, id, cancellationToken);
    }

    [McpServerTool(Name = "stop_scenario", Title = "Stop fault scenario", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Stops a fault scenario; faults, power and lines return to the state before it.")]
    [Authorize(Policy = Permissions.ScenariosManage)]
    public static async Task<FaultScenarioDto> StopScenario(
        IQueryHandler<GetFaultScenariosQuery, IReadOnlyList<FaultScenarioDto>> scenarios,
        ICommandHandler<StopFaultScenarioCommand> handler,
        [Description("Id of the scenario.")] int id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new StopFaultScenarioCommand(id), cancellationToken)).ThrowIfFailed();
        return await Find(scenarios, id, cancellationToken);
    }

    private static async Task<FaultScenarioDto> Find(IQueryHandler<GetFaultScenariosQuery, IReadOnlyList<FaultScenarioDto>> scenarios, int id,
        CancellationToken cancellationToken) =>
        (await scenarios.Handle(new GetFaultScenariosQuery(), cancellationToken)).ValueOrThrow().First(s => s.Id == id);
}

/// <summary>Scenare poruch.</summary>
public sealed record ScenariosResult(IReadOnlyList<FaultScenarioDto> Scenarios);
