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

    [McpServerTool(Name = "send_frame", Title = "Send bytes as INISS", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Sends bytes to a line as if INISS sent them: the boards process them (log, statistics, recording) and their replies are returned instead "
                 + "of being sent. Only on a line INISS is not connected to. The bytes must be whole messages of the line's protocol including checksums; "
                 + "messages from get_frames show the format. Returns the replies and the boards that changed.")]
    [Authorize(Policy = Permissions.BoardsControl)]
    public static async Task<SentFrameDto> SendFrame(
        ICommandHandler<SendFrameCommand, SentFrameDto> handler,
        [Description("Line number.")] int line,
        [Description("Bytes as hexadecimal pairs; spaces and dashes are ignored, e.g. \"10 02 81 00 05 50 72 61 68 61 ...\". At most 4096 bytes.")] string hex,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(new SendFrameCommand(line, hex), cancellationToken)).ValueOrThrow();
}
