using System.ComponentModel;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Display;

namespace TableSimulator.Mcp.Results;

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
