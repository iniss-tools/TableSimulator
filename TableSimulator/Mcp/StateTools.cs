using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.State;
using TableSimulator.Contracts.State;

namespace TableSimulator.Mcp;

/// <summary>Nastroje stavu simulatora.</summary>
[McpServerToolType]
public static class StateTools
{
    [McpServerTool(Name = "get_status", Title = "Simulator status", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Overview of the simulator: every line with its connection status, last message time and traffic counters, the number of boards per line, "
                 + "and boards with a problem (simulated fault, power outage, or INISS sending messages meant for another manufacturer).")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<StatusResult> GetStatus(IQueryHandler<GetStateQuery, StateDto> handler, CancellationToken cancellationToken) =>
        McpMapper.ToStatus((await handler.Handle(new GetStateQuery(), cancellationToken)).ValueOrThrow());
}
