namespace TableSimulator.Core.Boards.History;

/// <summary>Co sa v ustredniach zmenilo od posledneho odberu.</summary>
public sealed record DeviceHistorySample(IReadOnlyList<LightSample> Lights, IReadOnlyList<AnnouncementSample> Announcements)
{
    /// <summary>Ziadna zmena.</summary>
    public bool IsEmpty => Lights.Count == 0 && Announcements.Count == 0;
}
