using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Frames;
using TableSimulator.Contracts.Frames;

namespace TableSimulator.Mcp;

/// <summary>Nastroje logu komunikacie.</summary>
[McpServerToolType]
public static class FramesTools
{
    /// <summary>Najviac sprav v jednom vysledku.</summary>
    public const int MaxLimit = 500;

    [McpServerTool(Name = "get_frames", Title = "Communication log", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Messages exchanged on the lines, oldest first: rx = from INISS to a board, tx = the board's reply. Each message has its decoded meaning "
                 + "(kind and arguments) and the raw bytes in hex. Only the most recent messages are kept in memory.")]
    [Authorize(Policy = Permissions.LogView)]
    public static async Task<FramesResult> GetFrames(
        IQueryHandler<GetLogQuery, FrameDto[]> handler,
        [Description("Only messages on this line.")] int? line = null,
        [Description("Only messages for this board address (use together with line).")] int? address = null,
        [Description("Only messages that were not processed normally (bad checksum, no board at the address, simulated fault, noise).")] bool problemsOnly = false,
        [Description("Only messages with a sequence number greater than this - pass 'next' from the previous call to read newer messages.")] long after = 0,
        [Description("Maximum number of messages (1-500); when more match, the newest are returned.")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(limit, MaxLimit, "frames.limit");
        var log = (await handler.Handle(new GetLogQuery(Math.Max(after, 0)), cancellationToken)).ValueOrThrow();
        return McpMapper.ToFrames(log, after, line, address, problemsOnly, limit);
    }
}
