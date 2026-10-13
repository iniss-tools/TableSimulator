using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.State;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.State;

namespace TableSimulator.Mcp;

/// <summary>Nastroje tabul - co ukazuju a ich zmeny.</summary>
[McpServerToolType]
public static class BoardTools
{
    private const string IdDescription = "Board id \"line-address\", e.g. \"1-12\".";

    [McpServerTool(Name = "list_boards", Title = "Boards", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Boards with what they show as text rows, their version, simulated fault and power state, and when the last message for them arrived.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<BoardsResult> ListBoards(
        IQueryHandler<GetStateQuery, StateDto> state,
        IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        [Description("Only boards on this line.")] int? line = null,
        [Description("Include the text rows of each board; false gives a shorter list.")] bool includeText = true,
        CancellationToken cancellationToken = default)
    {
        var boards = (await state.Handle(new GetStateQuery(), cancellationToken)).ValueOrThrow().Boards.Where(b => line is null || b.Line == line).ToList();
        var rows = includeText ? (await texts.Handle(new GetBoardTextsQuery(line), cancellationToken)).ValueOrThrow() : null;
        return McpMapper.ToBoards(boards, rows);
    }

    [McpServerTool(Name = "get_board", Title = "Board detail", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("One board: what it shows as text rows, brightness and clock set by INISS, simulated fault, message counters and version. "
                 + "raw = true adds the board settings and the raw content with fonts and character codes.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<BoardDetail> GetBoard(
        IQueryHandler<GetBoardQuery, BoardDto> board,
        IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        [Description(IdDescription)] string id,
        [Description("Include the board settings and the raw content.")] bool raw = false,
        CancellationToken cancellationToken = default)
    {
        var dto = (await board.Handle(new GetBoardQuery(id), cancellationToken)).ValueOrThrow();
        var text = (await texts.Handle(new GetBoardTextsQuery(dto.Line), cancellationToken)).ValueOrThrow().FirstOrDefault(t => t.Id == dto.Id);
        return McpMapper.ToBoardDetail(dto, text, raw);
    }

    [McpServerTool(Name = "wait_for_board_change", Title = "Wait for board change", ReadOnly = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Waits until a board changes (a new message from INISS or a change of its settings) and returns what it shows then. "
                 + "Pass the version from list_boards or get_board; when the board already has a newer version, it returns at once. "
                 + "changed = false means the timeout expired without a change.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<BoardChangeDto> WaitForBoardChange(
        IQueryHandler<WaitForBoardChangeQuery, BoardChangeDto> handler,
        [Description(IdDescription)] string id,
        [Description("Version the caller already knows (from list_boards or get_board).")] long afterVersion,
        [Description("How long to wait at most [s], 1-60.")] int timeoutSeconds = 30,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(new WaitForBoardChangeQuery(id, afterVersion, TimeSpan.FromSeconds(timeoutSeconds)), cancellationToken)).ValueOrThrow();
}
