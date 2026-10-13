using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Lines;
using TableSimulator.Application.State;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.State;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Mcp;

/// <summary>Nastroje liniek a nacitania stanice z INISSu.</summary>
[McpServerToolType]
public static class LineTools
{
    [McpServerTool(Name = "set_line", Title = "Create or change line", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Creates a line or changes an existing one; for an existing line only the given values change. A new line needs tableClass and endpoint. "
                 + "The line starts listening at once (a port used by another program makes its status error).")]
    [Authorize(Policy = Permissions.LinesManage)]
    public static async Task<LineSummary> SetLine(
        IQueryHandler<GetStateQuery, StateDto> state,
        ICommandHandler<SetLineCommand> handler,
        [Description("Line number 1-100.")] int number,
        [Description("INISS table class, selects the protocol: 1 Elektrocas LCD, 2 ERS, 3 FERS, 4 ELEN, 5 LCD1/ERP, 10 APEL, 128 remote INISS, 129 ELSVO, 130 ITP.")]
        int? tableClass = null,
        [Description("Where INISS connects: TCP://0.0.0.0:47001, UDP://..., \\\\.\\PIPE\\name (INISS creates the pipe) or COM3:9600,n,8,1.")]
        string? endpoint = null,
        [Description("Name for the operator.")] string? name = null,
        [Description("display = boards answer like real ones; monitor = only listen, real boards answer.")] LineMode? mode = null,
        [Description("Create a board when a message comes for an unknown address.")] bool? autoLearn = null,
        [Description("Manufacturer of auto-created boards (e.g. ELEN, ELEN16, LCD1, APEL); default the first one of the class.")] string? manufacturer = null,
        CancellationToken cancellationToken = default)
    {
        var current = (await state.Handle(new GetStateQuery(), cancellationToken)).ValueOrThrow().Lines.FirstOrDefault(l => l.Number == number);
        var request = McpRequests.Line(current, number, tableClass, endpoint, name, mode, autoLearn, manufacturer);
        (await handler.Handle(new SetLineCommand(number, request), cancellationToken)).ThrowIfFailed();
        return McpMapper.ToStatus((await state.Handle(new GetStateQuery(), cancellationToken)).ValueOrThrow()).Lines.First(l => l.Number == number);
    }

    [McpServerTool(Name = "delete_line", Title = "Delete line", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Deletes a line together with all its boards.")]
    [Authorize(Policy = Permissions.LinesManage)]
    public static async Task<DoneResult> DeleteLine(
        ICommandHandler<DeleteLineCommand> handler,
        [Description("Line number.")] int number,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new DeleteLineCommand(number), cancellationToken)).ThrowIfFailed();
        return new DoneResult(string.Create(CultureInfo.InvariantCulture, $"Line {number} and its boards were deleted."));
    }

    [McpServerTool(Name = "get_iniss_station", Title = "Station in INISS", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Preview of the station from the INISS installation set in the simulator: timetables, lines and their boards, boards INISS has but the "
                 + "simulator does not support, the log folder and the XML boards. Nothing is changed; import_iniss_station loads it.")]
    [Authorize(Policy = Permissions.LinesManage)]
    public static async Task<InissStationDto> GetInissStation(IQueryHandler<GetInissStationQuery, InissStationDto> handler, CancellationToken cancellationToken) =>
        (await handler.Handle(new GetInissStationQuery(), cancellationToken)).ValueOrThrow();

    [McpServerTool(Name = "import_iniss_station", Title = "Import station from INISS", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Loads lines and boards from the INISS installation set in the simulator: missing ones are added, settings of existing lines are replaced, "
                 + "existing boards are kept. New boards get a face (board front) from the station data. INISS itself is not changed.")]
    [Authorize(Policy = Permissions.LinesManage)]
    public static async Task<StationImportedDto> ImportInissStation(
        ICommandHandler<ImportInissStationCommand, StationImportedDto> handler,
        [Description("Language of the labels on created board faces.")] StationFaceLanguage faceLanguage = StationFaceLanguage.Sk,
        [Description("Create faces also for boards that already exist.")] bool facesForExisting = false,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(new ImportInissStationCommand(new InissStationImportRequest(faceLanguage, facesForExisting)), cancellationToken)).ValueOrThrow();
}
