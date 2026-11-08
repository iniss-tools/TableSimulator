using System.ComponentModel;
using TableSimulator.Contracts.AkSignal;

namespace TableSimulator.Mcp.Results;

/// <summary>Historia ustredni AK Signal za obdobie.</summary>
public sealed record AkSignalHistoryResult(
    DateTimeOffset From,
    DateTimeOffset To,
    [property: Description("Light states: the state at the start of the period and every change; the last ones when truncated.")]
    IReadOnlyList<LightStateDto> States,
    [property: Description("Finished announcements started in the period; the last ones when truncated.")]
    IReadOnlyList<AnnouncementHistoryDto> Announcements,
    [property: Description("Light log of the INISS2 server: why the server switched a light (train, manual, state); the last ones when truncated.")]
    IReadOnlyList<ServerLightEventDto> ServerEvents,
    [property: Description("Some list had more entries than the limit.")]
    bool Truncated);
