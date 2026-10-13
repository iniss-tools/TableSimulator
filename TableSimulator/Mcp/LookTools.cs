using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
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
using TableSimulator.Core.Boards;

namespace TableSimulator.Mcp;

/// <summary>Nastroje vzhladu tabul: vzhlady, predlohy ciel, pisma a vzhlad jednej tabule.</summary>
[McpServerToolType]
public static class LookTools
{
    [McpServerTool(Name = "list_designs", Title = "Board designs", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Designs of boards - how the dots, frame, plate and flaps look (built-in and custom). A custom design with the id of a built-in one overrides it.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<DesignsResult> ListDesigns(IQueryHandler<GetDesignsQuery, DesignInfoDto[]> handler, CancellationToken cancellationToken) =>
        new((await handler.Handle(new GetDesignsQuery(), cancellationToken)).ValueOrThrow());

    [McpServerTool(Name = "get_design", Title = "Board design", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Whole design: pitch and size of dots, shape, colors per ELEN font color (0 none, 1 red, 2 green, 3 yellow, mono), off dots, glow, background, "
                 + "plate, frame, row gaps, module seams, header fonts and colors, flap material. Colors are #rrggbb.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<DesignDto> GetDesign(
        IQueryHandler<GetDesignQuery, DesignDto> handler,
        [Description("Design id.")] string id,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(new GetDesignQuery(id), cancellationToken)).ValueOrThrow();

    [McpServerTool(Name = "save_design", Title = "Save board design", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Saves a custom design: the whole design (as from get_design), or a copy of another design (copyFrom), optionally renamed. "
                 + "Saving under the id of a built-in design overrides it; deleting the custom one brings the built-in back. Boards using it redraw at once.")]
    [Authorize(Policy = Permissions.DesignsManage)]
    public static async Task<DesignDto> SaveDesign(
        IQueryHandler<GetDesignQuery, DesignDto> get,
        ICommandHandler<SetDesignCommand> handler,
        [Description("Design id: 1-40 lowercase letters, digits and hyphens.")] string id,
        [Description("The whole design; omit it when copying.")] SetDesignRequest? design = null,
        [Description("Id of a design to copy.")] string? copyFrom = null,
        [Description("New name for the operator.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        var source = design ?? (copyFrom is null ? null : (await get.Handle(new GetDesignQuery(copyFrom), cancellationToken)).ValueOrThrow().Design)
            ?? throw new McpException("designs.input: Pass the design or copyFrom.");
        (await handler.Handle(new SetDesignCommand(id, name is null ? source : source with { Name = name }), cancellationToken)).ThrowIfFailed();
        return (await get.Handle(new GetDesignQuery(id), cancellationToken)).ValueOrThrow();
    }

    [McpServerTool(Name = "delete_design", Title = "Delete board design", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Deletes a custom design; a built-in design with the same id is used again. Boards using a deleted design get the default one.")]
    [Authorize(Policy = Permissions.DesignsManage)]
    public static async Task<DoneResult> DeleteDesign(
        ICommandHandler<DeleteDesignCommand> handler,
        [Description("Design id.")] string id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new DeleteDesignCommand(id), cancellationToken)).ThrowIfFailed();
        return new DoneResult($"Custom design {id} was deleted.");
    }

    [McpServerTool(Name = "list_faces", Title = "Board faces", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Faces (board fronts): areas, labels, clocks, symbols and images around the dot matrix, shared by boards via profile.face.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<FacesResult> ListFaces(IQueryHandler<GetFacesQuery, BoardFaceDto[]> handler, CancellationToken cancellationToken) =>
        new((await handler.Handle(new GetFacesQuery(), cancellationToken)).ValueOrThrow()
            .Select(f => new FaceSummary(f.Id, f.Name, f.BuiltIn, f.Elements.Count)).ToList());

    [McpServerTool(Name = "get_face", Title = "Board face", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Whole face: margins around the matrix, row groups, module width and elements. Positions and sizes are in matrix dots, measured from the "
                 + "edge of the anchor (face, matrix or a row) given by the alignment. Texts may contain variables {key} filled from profile.faceValues.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<BoardFaceDto> GetFace(
        IQueryHandler<GetFacesQuery, BoardFaceDto[]> handler,
        [Description("Face id.")] string id,
        CancellationToken cancellationToken = default) =>
        await FindFace(handler, id, cancellationToken);

    [McpServerTool(Name = "save_face", Title = "Save board face", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Saves a custom face: the whole face (as from get_face), or a copy of another face (copyFrom), optionally renamed. "
                 + "Built-in faces cannot be changed, only copied. Boards using the face redraw at once.")]
    [Authorize(Policy = Permissions.DesignsManage)]
    public static async Task<BoardFaceDto> SaveFace(
        IQueryHandler<GetFacesQuery, BoardFaceDto[]> faces,
        ICommandHandler<SetFaceCommand> handler,
        [Description("Face id: lowercase letters without diacritics, digits and hyphens.")] string id,
        [Description("The whole face; omit it when copying.")] BoardFaceDto? face = null,
        [Description("Id of a face to copy.")] string? copyFrom = null,
        [Description("New name for the operator.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        var source = face ?? (copyFrom is null ? null : await FindFace(faces, copyFrom, cancellationToken))
            ?? throw new McpException("face.input: Pass the face or copyFrom.");
        var saved = source with { Id = id, BuiltIn = false, Name = name ?? source.Name };
        (await handler.Handle(new SetFaceCommand(id, saved), cancellationToken)).ThrowIfFailed();
        return await FindFace(faces, id, cancellationToken);
    }

    [McpServerTool(Name = "delete_face", Title = "Delete board face", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Deletes a custom face; a face used by boards cannot be deleted.")]
    [Authorize(Policy = Permissions.DesignsManage)]
    public static async Task<DoneResult> DeleteFace(
        ICommandHandler<DeleteFaceCommand> handler,
        [Description("Face id.")] string id,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new DeleteFaceCommand(id), cancellationToken)).ThrowIfFailed();
        return new DoneResult($"Face {id} was deleted.");
    }

    [McpServerTool(Name = "list_fonts", Title = "Board fonts", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("ELEN font files (.bin) in the fonts folder set in the simulator with their fonts, and the dot fonts for LCD1/ERP character boards.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<FontsResult> ListFonts(
        IQueryHandler<GetFontsQuery, string[]> files,
        IQueryHandler<GetFontQuery, FontFileDto> file,
        CancellationToken cancellationToken)
    {
        var result = new List<FontFileSummary>();
        foreach (var name in (await files.Handle(new GetFontsQuery(), cancellationToken)).ValueOrThrow())
        {
            // necitatelny subor vynecha - zoznam ostatnych ma prednost
            if ((await file.Handle(new GetFontQuery(name), cancellationToken)) is { IsSuccess: true, Value: var font })
                result.Add(new FontFileSummary(font.File, font.Name, font.Fonts.Select((f, i) => new FontSummary(i, f.Name, f.Height, f.MaxWidth, f.Proportional)).ToList()));
        }

        return new FontsResult(result, CharFont.Fonts.ToList());
    }

    [McpServerTool(Name = "set_board_look", Title = "Board look", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Changes how one board looks: its design and a partial change of its profile (JSON merge patch - only the given keys change, null removes "
                 + "a key). Useful profile keys: face, faceValues, fontFile, fontMap, charFonts (font number -> {font, bold}), monochrome, swapRedGreen, "
                 + "header, movingFont, alternationMs, blinkOnMs, blinkOffMs, scrollSpeed, rotationMs, flaps. get_board with raw = true shows the current "
                 + "profile. Returns the new profile and a page showing the board.")]
    [Authorize(Policy = Permissions.LinesManage)]
    public static async Task<BoardLookResult> SetBoardLook(
        IQueryHandler<GetBoardQuery, BoardDto> board,
        ICommandHandler<SetBoardCommand> handler,
        IHttpContextAccessor http,
        [Description("Board id \"line-address\".")] string id,
        [Description("Design id; empty = default design of the manufacturer; omit to keep.")] string? design = null,
        [Description("Partial change of the profile (JSON merge patch).")] JsonElement? profile = null,
        CancellationToken cancellationToken = default)
    {
        var current = (await board.Handle(new GetBoardQuery(id), cancellationToken)).ValueOrThrow();
        var request = new SetBoardRequest
        {
            Line = current.Line,
            Address = current.Address,
            Manufacturer = current.Manufacturer,
            Name = current.Name,
            Profile = profile is { } patch ? McpRequests.PatchProfile(current.Profile, patch) : current.Profile,
            Design = design is null ? current.Design : design.Length == 0 ? null : design,
            Fault = current.Fault,
            SecondAddress = current.SecondAddress
        };
        (await handler.Handle(new SetBoardCommand(current.Line, current.Address, request), cancellationToken)).ThrowIfFailed();
        var saved = (await board.Handle(new GetBoardQuery(id), cancellationToken)).ValueOrThrow();
        return new BoardLookResult(saved.Id, saved.Design, saved.Profile, McpRequests.ViewUrl(http.HttpContext?.Request, $"/view/{Uri.EscapeDataString(saved.Id)}"));
    }

    private static async Task<BoardFaceDto> FindFace(IQueryHandler<GetFacesQuery, BoardFaceDto[]> handler, string id, CancellationToken cancellationToken) =>
        (await handler.Handle(new GetFacesQuery(), cancellationToken)).ValueOrThrow().FirstOrDefault(f => f.Id == id)
        ?? throw new McpException($"face.notFound: Face '{id}' does not exist.");
}
