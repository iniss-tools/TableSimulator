using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.Designs;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Mcp;

/// <summary>Nastroje so zobrazenim v klientovi s MCP Apps (VS Code, aplikacia Claude) - zive tabule priamo v chate.</summary>
[McpServerToolType]
public static class AppTools
{
    [McpServerTool(Name = "show_board", Title = "Show board", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Shows a board live in the chat in clients with MCP Apps (e.g. VS Code); the view refreshes itself when the board changes. "
                 + "Other clients get the same result as get_board.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    [McpAppUi(ResourceUri = BoardApp.Uri)]
    public static Task<BoardDetail> ShowBoard(
        IQueryHandler<GetBoardQuery, BoardDto> board,
        IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>> texts,
        IQueryHandler<GetDesignsQuery, DesignInfoDto[]> designs,
        [Description("Board id \"line-address\", e.g. \"1-12\".")] string id,
        CancellationToken cancellationToken = default) =>
        BoardTools.Detail(board, texts, designs, id, false, cancellationToken);

    [McpServerTool(Name = "board_view", Title = "Board view data", ReadOnly = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Used by the board view of show_board: waits until the board changes and returns what it shows.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    [McpAppUi(ResourceUri = BoardApp.Uri, Visibility = [McpUiToolVisibility.App])]
    public static async Task<BoardChangeDto> BoardView(
        IQueryHandler<WaitForBoardChangeQuery, BoardChangeDto> handler,
        [Description("Board id \"line-address\".")] string id,
        [Description("Version the view shows.")] long afterVersion,
        [Description("How long to wait at most [s], 1-60.")] int timeoutSeconds = 25,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(new WaitForBoardChangeQuery(id, afterVersion, TimeSpan.FromSeconds(timeoutSeconds)), cancellationToken)).ValueOrThrow();
}
