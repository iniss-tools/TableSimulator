using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.Designs;
using TableSimulator.Application.State;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Designs;
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
        IQueryHandler<GetDesignsQuery, DesignInfoDto[]> designs,
        IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        [Description(IdDescription)] string id,
        [Description("Include the board settings and the raw content.")] bool raw = false,
        CancellationToken cancellationToken = default) =>
        await Detail(board, texts, designs, id, raw, cancellationToken);

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

    [McpServerTool(Name = "set_board", Title = "Create or change board", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Creates a board on a line or changes an existing one; for an existing board only the given values change. A new board needs a manufacturer "
                 + "and gets the default size of the manufacturer. Look, face and fonts are not changed here.")]
    [Authorize(Policy = Permissions.LinesManage)]
    public static async Task<BoardDetail> SetBoard(
        IQueryHandler<GetBoardQuery, BoardDto> board,
        IQueryHandler<GetDesignsQuery, DesignInfoDto[]> designs,
        IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        ICommandHandler<SetBoardCommand> handler,
        [Description("Line of the board.")] int line,
        [Description("Address of the board on the line.")] int address,
        [Description("Manufacturer (e.g. ELEN, ELEN16, LCD, LCD1, ERP, FERS, ERS, APEL, ELSVO); must belong to the class of the line.")] string? manufacturer = null,
        [Description("Name for the operator.")] string? name = null,
        [Description("Width of the matrix in dots.")] int? width = null,
        [Description("Number of rows.")] int? rows = null,
        [Description("Height of one row in dots.")] int? rowHeight = null,
        [Description("Second address of a two-sided board; 0 = none.")] int? secondAddress = null,
        CancellationToken cancellationToken = default)
    {
        var id = BoardId(line, address);
        var current = await board.Handle(new GetBoardQuery(id), cancellationToken);
        var request = McpRequests.Board(current.IsSuccess ? current.Value : null, line, address, manufacturer, name, width, rows, rowHeight, secondAddress);
        (await handler.Handle(new SetBoardCommand(line, address, request), cancellationToken)).ThrowIfFailed();
        return await Detail(board, texts, designs, id, false, cancellationToken);
    }

    [McpServerTool(Name = "delete_board", Title = "Delete board", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Deletes a board. With auto-learning on its line the next message for its address creates it again.")]
    [Authorize(Policy = Permissions.LinesManage)]
    public static async Task<DoneResult> DeleteBoard(
        ICommandHandler<DeleteBoardCommand> handler,
        [Description(IdDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new DeleteBoardCommand(id), cancellationToken)).ThrowIfFailed();
        return new DoneResult($"Board {id} was deleted.");
    }

    [McpServerTool(Name = "set_board_fault", Title = "Board fault", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Makes a board simulate a fault: noReply (does not answer), nak (rejects messages), delay (answers late), garbage (answers nonsense), "
                 + "status (reports a fault in its status reply, only boards with a status reply); none ends the fault. A running fault scenario may change it again.")]
    [Authorize(Policy = Permissions.BoardsControl)]
    public static async Task<BoardDetail> SetBoardFault(
        IQueryHandler<GetBoardQuery, BoardDto> board,
        IQueryHandler<GetDesignsQuery, DesignInfoDto[]> designs,
        IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        ICommandHandler<SetBoardFaultCommand> handler,
        [Description(IdDescription)] string id,
        [Description("Kind of the fault; none = no fault.")] FaultKind kind,
        [Description("Reply delay for delay [ms].")] int delayMs = 0,
        [Description("Status bits for status.")] int status = 0,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new SetBoardFaultCommand(id, new BoardFaultDto(kind, delayMs, status)), cancellationToken)).ThrowIfFailed();
        return await Detail(board, texts, designs, id, false, cancellationToken);
    }

    [McpServerTool(Name = "clear_board", Title = "Clear board", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Clears what a board shows; INISS fills it again with its next messages.")]
    [Authorize(Policy = Permissions.BoardsControl)]
    public static async Task<BoardDetail> ClearBoard(
        IQueryHandler<GetBoardQuery, BoardDto> board,
        IQueryHandler<GetDesignsQuery, DesignInfoDto[]> designs,
        IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        ICommandHandler<ClearBoardCommand> handler,
        [Description(IdDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new ClearBoardCommand(id), cancellationToken)).ThrowIfFailed();
        return await Detail(board, texts, designs, id, false, cancellationToken);
    }

    private static string BoardId(int line, int address) => string.Create(CultureInfo.InvariantCulture, $"{line}-{address}");

    private static async Task<BoardDetail> Detail(IQueryHandler<GetBoardQuery, BoardDto> board, IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        IQueryHandler<GetDesignsQuery, DesignInfoDto[]> designs, string id, bool raw, CancellationToken cancellationToken)
    {
        var dto = (await board.Handle(new GetBoardQuery(id), cancellationToken)).ValueOrThrow();
        var text = (await texts.Handle(new GetBoardTextsQuery(dto.Line), cancellationToken)).ValueOrThrow().FirstOrDefault(t => t.Id == dto.Id);
        var ids = (await designs.Handle(new GetDesignsQuery(), cancellationToken)).ValueOrThrow().Select(x => x.Id).ToList();
        return McpMapper.ToBoardDetail(dto, text, raw, ids);
    }
}
