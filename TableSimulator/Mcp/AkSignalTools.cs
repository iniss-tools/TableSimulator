using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.Devices;
using TableSimulator.Application.State;
using TableSimulator.Contracts.AkSignal;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Display;
using TableSimulator.Contracts.State;

namespace TableSimulator.Mcp;

/// <summary>Nastroje ustredni AK Signal servera INISS2 - svetlo, snimac tmy, stavy a historia.</summary>
[McpServerToolType]
public static class AkSignalTools
{
    private const string IdDescription = "Board id \"line-address\" of the controller, e.g. \"6-5\"; AKSIGNALUH controllers have address 0.";

    /// <summary>Najdlhsie obdobie historie v nastroji [min] - 7 dni.</summary>
    private const int MaxMinutes = 7 * 24 * 60;

    private const int MaxLimit = 1000;

    [McpServerTool(Name = "get_aksignal", Title = "AK Signal controllers", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("AK Signal controllers through which the INISS2 server switches platform lights and public address: light mode set by the server, "
                 + "twilight sensor, conditions (faults, higher priority) including those of a running fault scenario, the state as the controller replies, "
                 + "whether the server still asks, and announcements playing now.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<AkSignalControllersResult> GetAkSignal(
        IQueryHandler<GetStateQuery, StateDto> state,
        [Description("Only this controller; null = all.")] string? id = null,
        CancellationToken cancellationToken = default)
    {
        var boards = (await state.Handle(new GetStateQuery(), cancellationToken)).ValueOrThrow().Boards
            .Where(b => b.Display.AkSignal is not null && (id is null || b.Id == id))
            .ToList();
        if (id is not null && boards.Count == 0)
            throw new McpException(McpResults.Describe(BoardErrors.NotAkSignal(id)));
        return new AkSignalControllersResult(boards.Select(ToController).ToList());
    }

    [McpServerTool(Name = "set_aksignal", Title = "Set AK Signal controller", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Changes the twilight sensor and the conditions of an AK Signal controller that the server reads but does not set; only the given values change. "
                 + "The light mode is set by the server only. For a condition that lasts only a while use a fault scenario with a device step.")]
    [Authorize(Policy = Permissions.BoardsControl)]
    public static async Task<AkSignalController> SetAkSignal(
        IQueryHandler<GetBoardQuery, BoardDto> board,
        IQueryHandler<GetStateQuery, StateDto> state,
        ICommandHandler<SetAkSignalCommand> handler,
        [Description(IdDescription)] string id,
        [Description("Sensor mode: sun (by sunrise and sunset at the simulator location), dark or light.")] TwilightMode? sensor = null,
        [Description("Sensor offset against sunrise and sunset [min], -240 to 240; positive shortens the darkness.")] int? sensorOffsetMinutes = null,
        [Description("Lights fault (the controller does not report its lights as OK).")] bool? lightsFault = null,
        [Description("An announcement with higher priority (e.g. local microphone) runs; it cancels the announcement of the server.")] bool? highPriority = null,
        [Description("Power error on the audio output.")] bool? outputPowerError = null,
        [Description("Amplifier power supply missing.")] bool? ampSupplyMissing = null,
        [Description("Communication error with the amplifier (AKSIGNALUH only).")] bool? ampCommError = null,
        [Description("Sound bank not synchronized (AKSIGNALUH only).")] bool? bankNotSynced = null,
        [Description("After how many seconds without a query the controller reports that the server does not ask.")] int? silenceSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var current = (await board.Handle(new GetBoardQuery(id), cancellationToken)).ValueOrThrow();
        if (current.Display.AkSignal is null)
            throw new McpException(McpResults.Describe(BoardErrors.NotAkSignal(id)));
        var profile = current.Profile.AkSignal ?? new AkSignalProfileDto();
        var c = profile.Conditions;
        var request = new AkSignalSettingsRequest(
            new TwilightSensorDto(sensor ?? profile.Sensor.Mode, sensorOffsetMinutes ?? profile.Sensor.OffsetMinutes),
            new AkSignalConditionsDto(lightsFault ?? c.LightsFault, highPriority ?? c.HighPriority, outputPowerError ?? c.OutputPowerError,
                ampSupplyMissing ?? c.AmpSupplyMissing, ampCommError ?? c.AmpCommError, bankNotSynced ?? c.BankNotSynced))
        {
            SilenceSeconds = silenceSeconds
        };
        (await handler.Handle(new SetAkSignalCommand(id, request), cancellationToken)).ThrowIfFailed();
        return (await GetAkSignal(state, id, cancellationToken)).Controllers[0];
    }

    [McpServerTool(Name = "get_aksignal_history", Title = "AK Signal history", ReadOnly = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("History of AK Signal controllers and remote INISS public address: light states (on, auto, manual, dark, fault, server silent, power), "
                 + "finished announcements and the light log of the INISS2 server (which train made the server switch a light) for the last minutes. "
                 + "The simulator keeps the history for 30 days.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<AkSignalHistoryResult> GetAkSignalHistory(
        IQueryHandler<GetDeviceHistoryQuery, DeviceHistoryDto> handler,
        TimeProvider time,
        [Description("Only this controller or remote INISS public address; null = all.")] string? id = null,
        [Description("How many minutes back from now, 1-10080.")] int minutes = 60,
        [Description("At most this many entries of each list (the last ones), 1-1000.")] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(limit, MaxLimit, "history.limit");
        if (minutes is < 1 or > MaxMinutes)
            throw new McpException($"{GetDeviceHistoryQueryValidator.Range}: The minutes must be 1-{MaxMinutes}.");
        var to = time.GetUtcNow();
        var history = (await handler.Handle(new GetDeviceHistoryQuery(to.AddMinutes(-minutes), to, id), cancellationToken)).ValueOrThrow();
        var truncated = history.Lights.Count > limit || history.Announcements.Count > limit || history.Server.Count > limit;
        return new AkSignalHistoryResult(history.From, history.To, history.Lights.TakeLast(limit).ToList(), history.Announcements.TakeLast(limit).ToList(),
            history.Server.TakeLast(limit).ToList(), truncated);
    }

    private static AkSignalController ToController(BoardDto board)
    {
        var state = board.Display.AkSignal!;
        var profile = board.Profile.AkSignal ?? new AkSignalProfileDto();
        var mode = state.Manual ? "manual" : state.AutoMode ? "auto" : "off";
        return new AkSignalController(board.Id, board.Name, board.Manufacturer, board.PoweredOff, state.LightOn, mode, state.Dark,
            state.Scenario?.Sensor ?? profile.Sensor.Mode, profile.Sensor.OffsetMinutes, state.DarkChangesAt, state.Conditions, state.Scenario,
            state.ServerSilent, LastQuery(state), state.LightState, state.AudioState,
            profile.Lights.Select(l => l.Name).ToList(), profile.Audio.Select(a => a.Name).ToList(),
            state.Announcements.Where(a => a.Result == AnnouncementResult.Playing).ToList());
    }

    private static DateTimeOffset? LastQuery(AkSignalDto state) => (state.LightQueryAt, state.AudioQueryAt) switch
    {
        ({ } light, { } audio) => light > audio ? light : audio,
        (var light, var audio) => light ?? audio
    };
}
