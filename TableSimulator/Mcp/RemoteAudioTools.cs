using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.State;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Display;
using TableSimulator.Contracts.State;

namespace TableSimulator.Mcp;

/// <summary>Rozhlas vzdialeneho INISSu (linka triedy 128) - fronty, hlasenia a ich prerusenie.</summary>
public sealed record RemoteAudioResult(IReadOnlyList<RemoteAudioDevice> Devices);

/// <summary>Rozhlas jedneho vzdialeneho INISSu.</summary>
public sealed record RemoteAudioDevice(
    [property: Description("Board id \"line-address\" of the device (address 0 on a class 128 line).")]
    string Id,
    string Name,
    int Line,
    [property: Description("Simulated power outage: queues answer as before, without announcements.")]
    bool PoweredOff,
    [property: Description("Simulated fault of the remote INISS: noReply = it does not answer (the controlling INISS logs remote timeout), delay = late answers.")]
    FaultKind Fault,
    [property: Description("Last message of the controlling INISS with queues; null = none yet.")]
    DateTimeOffset? LastQueryAt,
    [property: Description("Announcement queues; name \"\" = the only queue of the line.")]
    IReadOnlyList<RemoteQueueDto> Queues,
    [property: Description("Last announcements, newest last; result cancelled = broken by the operator (remote break).")]
    IReadOnlyList<AnnouncementDto> Announcements);

[McpServerToolType]
public static class RemoteAudioTools
{
    private const int MaxAnnouncements = 50;

    [McpServerTool(Name = "get_remote_audio", Title = "Remote INISS public address", ReadOnly = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Public address of remote INISS lines (class 128) as INISS 3.39 sends it: queues (off, idle, playing), the announcement playing "
                 + "with its title, circuit, recordings (language/group/sound keys of the sound bank) and length from the bank, and last announcements. "
                 + "The simulator creates the device at address 0 with the first queue message.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<RemoteAudioResult> GetRemoteAudio(
        IQueryHandler<GetStateQuery, StateDto> state,
        [Description("Only this device; null = all.")] string? id = null,
        [Description("At most this many last announcements per device, 1-50.")] int announcements = 10,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(announcements, MaxAnnouncements, "remoteAudio.limit");
        var boards = (await state.Handle(new GetStateQuery(), cancellationToken)).ValueOrThrow().Boards
            .Where(b => b.Display.RemoteAudio is not null && (id is null || b.Id == id))
            .ToList();
        if (id is not null && boards.Count == 0)
            throw new McpException(McpResults.Describe(BoardErrors.NotRemoteAudio(id)));
        return new RemoteAudioResult(boards.Select(b =>
        {
            var audio = b.Display.RemoteAudio!;
            return new RemoteAudioDevice(b.Id, b.Name, b.Line, b.PoweredOff, b.Fault.Kind, audio.LastQueryAt, audio.Queues,
                audio.Announcements.TakeLast(announcements).ToList());
        }).ToList());
    }

    [McpServerTool(Name = "break_announcement", Title = "Break remote announcement", ReadOnly = false, Destructive = false, Idempotent = false,
        OpenWorld = false, UseStructuredContent = true)]
    [Description("Breaks the announcement playing on a remote INISS queue: the next answer has no state of the queue, so the controlling INISS "
                 + "ends the announcement and writes remote break to its report archive. For a remote timeout set the fault noReply on the device.")]
    [Authorize(Policy = Permissions.BoardsControl)]
    public static async Task<RemoteAudioDevice> BreakAnnouncement(
        IQueryHandler<GetStateQuery, StateDto> state,
        ICommandHandler<BreakAnnouncementCommand> handler,
        [Description("Board id \"line-address\" of the remote INISS public address, e.g. \"4-0\".")] string id,
        [Description("Queue name; \"\" = the only queue of the line; null = every playing queue.")] string? queue = null,
        CancellationToken cancellationToken = default)
    {
        (await handler.Handle(new BreakAnnouncementCommand(id, queue), cancellationToken)).ThrowIfFailed();
        return (await GetRemoteAudio(state, id, 10, cancellationToken)).Devices[0];
    }
}
