using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Recordings;
using TableSimulator.Application.Replay;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Mcp;

/// <summary>Nastroje zaznamov komunikacie (.tsrec).</summary>
[McpServerToolType]
public static class RecordingTools
{
    /// <summary>Najviac krokov porovnania jednej tabule.</summary>
    public const int MaxSteps = 500;

    /// <summary>Najviac stavov tabule zo zaznamu.</summary>
    public const int MaxStates = 1000;

    [McpServerTool(Name = "list_recordings", Title = "Recordings", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Recordings of the communication between INISS and the boards with their length [s], lines, number of messages and note, "
                 + "plus the state of the recorder and of a running replay.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<RecordingsDto> ListRecordings(IQueryHandler<GetRecordingsQuery, RecordingsDto> handler, CancellationToken cancellationToken) =>
        (await handler.Handle(new GetRecordingsQuery(), cancellationToken)).ValueOrThrow();

    [McpServerTool(Name = "compare_recordings", Title = "Compare recordings", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Compares what the boards showed in two recordings, e.g. before and after a timetable change. Recordings are replayed without waiting; "
                 + "each board becomes a sequence of states (changes within 1 s are one state) and the sequences of A and B are aligned.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<ComparisonResult> CompareRecordings(
        IQueryHandler<CompareRecordingsQuery, RecordingComparisonDto> handler,
        [Description("Name of the first recording (e.g. before the change).")] string a,
        [Description("Name of the second recording (e.g. after the change).")] string b,
        [Description("Only this board (id \"line-address\").")] string? board = null,
        [Description("Leave out boards and steps that are the same in both recordings.")] bool onlyDifferences = true,
        [Description("Maximum number of steps per board (1-500).")] int maxSteps = 50,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(maxSteps, MaxSteps, "recording.maxSteps");
        var comparison = (await handler.Handle(new CompareRecordingsQuery(a, b), cancellationToken)).ValueOrThrow();
        if (board is not null && comparison.Boards.All(c => c.Id != board))
            throw new McpException($"recording.boardNotFound: Board '{board}' is in neither recording.");
        return McpMapper.ToComparison(comparison, board, onlyDifferences, maxSteps);
    }

    [McpServerTool(Name = "get_recording_board", Title = "Board in a recording", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("What one board showed during a recording: its states over time (changes within 1 s are one state). "
                 + "With time, only the state shown at that moment.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<RecordingBoardResult> GetRecordingBoard(
        IQueryHandler<GetRecordingTimelineQuery, BoardTimelineDto> handler,
        [Description("Name of the recording.")] string name,
        [Description("Board id \"line-address\".")] string board,
        [Description("Seconds from the start of the recording; only the state shown at that moment.")] double? time = null,
        [Description("Maximum number of states from the start (1-1000).")] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(limit, MaxStates, "recording.limit");
        var timeline = (await handler.Handle(new GetRecordingTimelineQuery(name, board), cancellationToken)).ValueOrThrow();
        return McpMapper.ToRecordingBoard(timeline, time, limit);
    }

    [McpServerTool(Name = "start_recording", Title = "Start recording", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Starts recording the communication of all lines (messages from INISS and replies of the boards) into a new recording; "
                 + "a running recording is finished first.")]
    [Authorize(Policy = Permissions.RecordingsManage)]
    public static async Task<RecorderStatusDto> StartRecording(
        ICommandHandler<StartRecordingCommand, RecorderStatusDto> handler,
        [Description("Note shown with the recording, e.g. what is being tested.")] string? note = null,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(new StartRecordingCommand(note), cancellationToken)).ValueOrThrow();

    [McpServerTool(Name = "stop_recording", Title = "Stop recording", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Finishes the running recording and saves it.")]
    [Authorize(Policy = Permissions.RecordingsManage)]
    public static async Task<RecorderStatusDto> StopRecording(ICommandHandler<StopRecordingCommand, RecorderStatusDto> handler, CancellationToken cancellationToken) =>
        (await handler.Handle(new StopRecordingCommand(), cancellationToken)).ValueOrThrow();

    [McpServerTool(Name = "play_recording", Title = "Play recording", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Plays a recording into the simulator: its messages go to the boards of the recorded lines in the original timing as if INISS sent them, "
                 + "replies of the boards are dropped. From a position other than 0 the boards of those lines are cleared and the earlier messages are "
                 + "processed at once. Calling it again restarts the replay, e.g. at another position or paused. The replay ends with stop_replay.")]
    [Authorize(Policy = Permissions.RecordingsManage)]
    public static async Task<ReplayStatusDto> PlayRecording(
        ICommandHandler<PlayRecordingCommand, ReplayStatusDto> handler,
        [Description("Name of the recording.")] string name,
        [Description("Speed, 1 = real time.")] double speed = 1,
        [Description("Play again from the start after the end.")] bool loop = false,
        [Description("Where to start [s from the start of the recording].")] double position = 0,
        [Description("Only prepare the position and pause.")] bool paused = false,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(new PlayRecordingCommand(name, speed, loop, position, paused), cancellationToken)).ValueOrThrow();

    [McpServerTool(Name = "stop_replay", Title = "Stop replay", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Stops the replay of a recording; the boards keep what they show.")]
    [Authorize(Policy = Permissions.RecordingsManage)]
    public static async Task<DoneResult> StopReplay(ICommandHandler<StopReplayCommand> handler, CancellationToken cancellationToken)
    {
        (await handler.Handle(new StopReplayCommand(), cancellationToken)).ThrowIfFailed();
        return new DoneResult("The replay was stopped.");
    }
}
