using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.Designs;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Fonts;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Designs;
using TableSimulator.Contracts.Faces;
using TableSimulator.Contracts.Fonts;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Infrastructure.Hosting;

namespace TableSimulator.Mcp;

/// <summary>Nastroje so zobrazenim v klientovi s MCP Apps (VS Code, aplikacia Claude) - zive tabule a virtualne tabule priamo v chate.</summary>
[McpServerToolType]
public static class AppTools
{
    [McpServerTool(Name = "show_board", Title = "Show board", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Shows a board live in the chat in clients with MCP Apps (e.g. VS Code); the view refreshes itself when the board changes. "
                 + "Other clients get the same result as get_board.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    [McpAppUi(ResourceUri = AppPages.BoardUri)]
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
    [McpAppUi(ResourceUri = AppPages.BoardUri, Visibility = [McpUiToolVisibility.App])]
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

    [McpServerTool(Name = "show_virtual_board", Title = "Show virtual board", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Shows a virtual board live in the chat in clients with MCP Apps (e.g. VS Code), drawn by its layout; the view refreshes itself when "
                 + "the XML, the definition or the layout changes. Boards with an XSL template are not drawn in the chat. Other clients get the same "
                 + "result as get_virtual_board.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    [McpAppUi(ResourceUri = AppPages.VirtualUri)]
    public static async Task<VirtualBoardContent> ShowVirtualBoard(
        IQueryHandler<GetVirtualViewQuery, VirtualViewDto> handler,
        IHttpContextAccessor http,
        [Description("Number of the virtual board.")] int id,
        CancellationToken cancellationToken = default) =>
        VirtualTools.Content((await handler.Handle(new GetVirtualViewQuery(id), cancellationToken)).ValueOrThrow(), http);

    [McpServerTool(Name = "virtual_view", Title = "Virtual board view data", ReadOnly = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Used by the view of show_virtual_board: waits until what the virtual board shows changes and returns it with its layout.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    [McpAppUi(ResourceUri = AppPages.VirtualUri, Visibility = [McpUiToolVisibility.App])]
    public static async Task<VirtualViewData> VirtualView(
        IQueryHandler<GetVirtualViewQuery, VirtualViewDto> handler,
        TimeProvider time,
        [Description("Number of the virtual board.")] int id,
        [Description("Hash of the content the view shows; null = first view.")] string? afterHash = null,
        [Description("How long to wait at most [s], 1-60.")] int timeoutSeconds = 25,
        CancellationToken cancellationToken = default)
    {
        if (timeoutSeconds is < 1 or > 60)
            throw new McpException("virtual.waitTimeout: The timeout must be 1 to 60 seconds.");
        // zmena XML sa prejavi az po prechode priecinkov, castejsie sa obsah zistovat nemusi
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var start = time.GetTimestamp();
        while (true)
        {
            var view = (await handler.Handle(new GetVirtualViewQuery(id), cancellationToken)).ValueOrThrow();
            var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(view, JsonSerializerOptions.Web)), 0, 16);
            if (hash != afterHash)
                return new VirtualViewData(true, hash, view);
            var left = timeout - time.GetElapsedTime(start);
            if (left <= TimeSpan.Zero)
                return new VirtualViewData(false, hash, view);
            await Task.Delay(left < XmlTableWatcherService.Interval ? left : XmlTableWatcherService.Interval, time, cancellationToken).ConfigureAwait(false);
        }
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

/// <summary>Udaje zobrazenia virtualnej tabule pre aplikaciu MCP Apps (web/src/mcp-app/VirtualApp.tsx).</summary>
public sealed record VirtualViewData(
    [property: Description("The content changed; false = the timeout expired.")]
    bool Changed,
    [property: Description("Hash of the content; pass it as afterHash to wait for the next change.")]
    string Hash,
    VirtualViewDto View);
