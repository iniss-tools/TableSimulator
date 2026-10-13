using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.Designs;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Fonts;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Designs;
using TableSimulator.Contracts.Faces;
using TableSimulator.Contracts.Fonts;

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
    [Description("Used by the board view of show_board: waits until the board changes and returns everything to draw it - the board, its design, "
                 + "face and ELEN font file (only when the view does not have it yet).")]
    [Authorize(Policy = Permissions.SimulatorView)]
    [McpAppUi(ResourceUri = BoardApp.Uri, Visibility = [McpUiToolVisibility.App])]
    public static async Task<BoardViewData> BoardView(
        IQueryHandler<WaitForBoardChangeQuery, BoardChangeDto> wait,
        IQueryHandler<GetBoardQuery, BoardDto> board,
        IQueryHandler<GetDesignsQuery, DesignInfoDto[]> designs,
        IQueryHandler<GetDesignQuery, DesignDto> design,
        IQueryHandler<GetFacesQuery, BoardFaceDto[]> faces,
        IQueryHandler<GetFontQuery, FontFileDto> font,
        [Description("Board id \"line-address\".")] string id,
        [Description("Version the view shows.")] long afterVersion = 0,
        [Description("Wait for a newer version; false = return the board at once (first view).")] bool waitForChange = true,
        [Description("How long to wait at most [s], 1-60.")] int timeoutSeconds = 25,
        [Description("ELEN font file the view already has; it is not sent again.")] string? knownFontFile = null,
        CancellationToken cancellationToken = default)
    {
        var changed = true;
        if (waitForChange)
            changed = (await wait.Handle(new WaitForBoardChangeQuery(id, afterVersion, TimeSpan.FromSeconds(timeoutSeconds)), cancellationToken)).ValueOrThrow().Changed;
        var dto = (await board.Handle(new GetBoardQuery(id), cancellationToken)).ValueOrThrow();
        var ids = (await designs.Handle(new GetDesignsQuery(), cancellationToken)).ValueOrThrow().Select(x => x.Id).ToList();
        // vzhlad, ktory sa neda precitat, doplni prehliadac predvolenym
        var used = await design.Handle(new GetDesignQuery(McpMapper.DesignInUse(dto, ids)), cancellationToken);
        var face = dto.Profile.Face is { Length: > 0 } faceId
            ? (await faces.Handle(new GetFacesQuery(), cancellationToken)).ValueOrThrow().FirstOrDefault(f => f.Id == faceId)
            : null;
        FontFileDto? fontFile = null;
        if (dto.Profile.FontFile is { Length: > 0 } file && file != knownFontFile)
            fontFile = (await font.Handle(new GetFontQuery(file), cancellationToken)) is { IsSuccess: true } loaded ? loaded.Value : null;
        return new BoardViewData(changed, dto, used.IsSuccess ? used.Value.Design : null, face, fontFile);
    }
}

/// <summary>Udaje zobrazenia tabule pre aplikaciu MCP Apps (web/src/mcp-app/board-view.ts).</summary>
public sealed record BoardViewData(
    [property: Description("The board changed; false = the timeout expired.")]
    bool Changed,
    BoardDto Board,
    [property: Description("Content of design.json of the design used; null = default of the renderer.")]
    SetDesignRequest? Design,
    BoardFaceDto? Face,
    [property: Description("ELEN font file of the board; null = none or the view has it already.")]
    FontFileDto? Font);
