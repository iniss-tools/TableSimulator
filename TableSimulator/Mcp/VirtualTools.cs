using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Mcp;

/// <summary>Nastroje virtualnych tabul - XML tabul INISSu na obrazovkach ako v INISSView.</summary>
[McpServerToolType]
public static class VirtualTools
{
    [McpServerTool(Name = "list_virtual_boards", Title = "Virtual boards", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Virtual boards - screens that show the XML boards INISS writes to its log folder (like INISSView): their definitions, the XML files found, "
                 + "the watched folders, layouts and XSL templates.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<VirtualOverviewResult> ListVirtualBoards(
        IQueryHandler<GetVirtualOverviewQuery, VirtualOverviewDto> handler,
        CancellationToken cancellationToken)
    {
        var overview = (await handler.Handle(new GetVirtualOverviewQuery(), cancellationToken)).ValueOrThrow();
        return new VirtualOverviewResult(overview.Boards, overview.Sources, overview.Folders,
            overview.Layouts.Select(l => new VirtualLayoutSummary(l.Id, l.Name, l.BuiltIn, l.Columns.Count)).ToList(), overview.DefaultLayout, overview.Templates);
    }

    [McpServerTool(Name = "get_virtual_board", Title = "Virtual board content", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("What a virtual board shows now: the records from its XML file as cells, the layout used and a page showing the board.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<VirtualBoardContent> GetVirtualBoard(
        IQueryHandler<GetVirtualViewQuery, VirtualViewDto> handler,
        IHttpContextAccessor http,
        [Description("Number of the virtual board.")] int id,
        CancellationToken cancellationToken = default) =>
        Content((await handler.Handle(new GetVirtualViewQuery(id), cancellationToken)).ValueOrThrow(), http);

    [McpServerTool(Name = "get_virtual_layout", Title = "Virtual board layout", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Whole layout of virtual boards: title ({name} = board name, {station} = station from the XML), clock, date, column headers, rows to fit, "
                 + "font, colors, columns (XML elements, label, width as fill, 12% or 6em, alignment, color text/accent/muted/delay or #rrggbb, bold, size) "
                 + "and alternation and blinking timing.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<VirtualLayoutDto> GetVirtualLayout(
        IQueryHandler<GetVirtualOverviewQuery, VirtualOverviewDto> handler,
        [Description("Layout id.")] string id,
        CancellationToken cancellationToken = default) =>
        await FindLayout(handler, id, cancellationToken);

    [McpServerTool(Name = "save_virtual_board", Title = "Save virtual board", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Creates a virtual board or changes an existing one by a partial change (JSON merge patch) of its definition: name, description, source "
                 + "(XML file name without extension), rowStart (first record from 1), rowCount (null = all), separator (text instead of # in station and "
                 + "direction; empty = parts alternate), template (XSL template; empty = by layout), parameter (attribute x for the template), layout "
                 + "(empty = default). Only the given keys change; null sets the default. A new board needs at least name and source.")]
    [Authorize(Policy = Permissions.VirtualManage)]
    public static async Task<VirtualBoardContent> SaveVirtualBoard(
        IQueryHandler<GetVirtualOverviewQuery, VirtualOverviewDto> overview,
        IQueryHandler<GetVirtualViewQuery, VirtualViewDto> view,
        ICommandHandler<SetVirtualBoardCommand> handler,
        IHttpContextAccessor http,
        [Description("Number of the virtual board (also in its page address #/virtual/<number>).")] int id,
        [Description("Partial change of the definition (JSON merge patch).")] JsonElement changes,
        CancellationToken cancellationToken = default)
    {
        var current = (await overview.Handle(new GetVirtualOverviewQuery(), cancellationToken)).ValueOrThrow().Boards.FirstOrDefault(b => b.Id == id);
        var request = McpRequests.Patch(current is null ? new SetVirtualBoardRequest() : ToRequest(current), changes, "virtual.board");
        (await handler.Handle(new SetVirtualBoardCommand(id, request), cancellationToken)).ThrowIfFailed();
        return Content((await view.Handle(new GetVirtualViewQuery(id), cancellationToken)).ValueOrThrow(), http);
    }

    [McpServerTool(Name = "delete_virtual_board", Title = "Delete virtual board", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Deletes a virtual board; devices showing it lose the page.")]
    [Authorize(Policy = Permissions.VirtualManage)]
    public static async Task<DoneResult> DeleteVirtualBoard(
        ICommandHandler<DeleteVirtualBoardCommand> handler,
        [Description("Number of the virtual board.")] int id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new DeleteVirtualBoardCommand(id), cancellationToken)).ThrowIfFailed();
        return new DoneResult(string.Create(CultureInfo.InvariantCulture, $"Virtual board {id} was deleted."));
    }

    [McpServerTool(Name = "save_virtual_layout", Title = "Save virtual board layout", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Saves a custom layout: starts from a copy of another layout (copyFrom), else from the existing custom layout with this id, else from "
                 + "defaults, and applies a partial change (JSON merge patch, keys as in get_virtual_layout; arrays like columns are replaced whole). "
                 + "Built-in layouts cannot be changed, only copied. Boards using the layout redraw at once.")]
    [Authorize(Policy = Permissions.VirtualManage)]
    public static async Task<VirtualLayoutDto> SaveVirtualLayout(
        IQueryHandler<GetVirtualOverviewQuery, VirtualOverviewDto> overview,
        ICommandHandler<SetVirtualLayoutCommand> handler,
        [Description("Layout id: lowercase letters without diacritics, digits and hyphens.")] string id,
        [Description("Id of a layout to copy.")] string? copyFrom = null,
        [Description("Partial change of the layout (JSON merge patch).")] JsonElement? changes = null,
        CancellationToken cancellationToken = default)
    {
        var layouts = (await overview.Handle(new GetVirtualOverviewQuery(), cancellationToken)).ValueOrThrow().Layouts;
        if (copyFrom is null && layouts.Any(l => l.Id == id && l.BuiltIn))
            throw new McpException($"virtual.layoutBuiltIn: Layout '{id}' is built in and cannot be changed; save a copy under another id (copyFrom).");
        var source = copyFrom is not null
            ? layouts.FirstOrDefault(l => l.Id == copyFrom) ?? throw new McpException($"virtual.layoutNotFound: Layout '{copyFrom}' does not exist.")
            : layouts.FirstOrDefault(l => l.Id == id && !l.BuiltIn) ?? new VirtualLayoutDto();
        var layout = changes is { } patch ? McpRequests.Patch(source, patch, "virtual.layout") : source;
        (await handler.Handle(new SetVirtualLayoutCommand(id, layout with { Id = id, BuiltIn = false }), cancellationToken)).ThrowIfFailed();
        return await FindLayout(overview, id, cancellationToken);
    }

    [McpServerTool(Name = "delete_virtual_layout", Title = "Delete virtual board layout", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Deletes a custom layout; a layout used by virtual boards cannot be deleted.")]
    [Authorize(Policy = Permissions.VirtualManage)]
    public static async Task<DoneResult> DeleteVirtualLayout(
        ICommandHandler<DeleteVirtualLayoutCommand> handler,
        [Description("Layout id.")] string id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new DeleteVirtualLayoutCommand(id), cancellationToken)).ThrowIfFailed();
        return new DoneResult($"Layout {id} was deleted.");
    }

    private static SetVirtualBoardRequest ToRequest(VirtualBoardDto board) => new()
    {
        Name = board.Name,
        Description = board.Description,
        Source = board.Source,
        RowCount = board.RowCount,
        RowStart = board.RowStart,
        Separator = board.Separator,
        Template = board.Template,
        Parameter = board.Parameter,
        Layout = board.Layout
    };

    private static VirtualBoardContent Content(VirtualViewDto view, IHttpContextAccessor http) =>
        new(view.Board, view.Source, view.Rows, view.Layout.Id, view.Page?.Error,
            McpRequests.ViewUrl(http.HttpContext?.Request, string.Create(CultureInfo.InvariantCulture, $"/virtual/{view.Board.Id}")));

    private static async Task<VirtualLayoutDto> FindLayout(IQueryHandler<GetVirtualOverviewQuery, VirtualOverviewDto> handler, string id,
        CancellationToken cancellationToken) =>
        (await handler.Handle(new GetVirtualOverviewQuery(), cancellationToken)).ValueOrThrow().Layouts.FirstOrDefault(l => l.Id == id)
        ?? throw new McpException($"virtual.layoutNotFound: Layout '{id}' does not exist.");
}
