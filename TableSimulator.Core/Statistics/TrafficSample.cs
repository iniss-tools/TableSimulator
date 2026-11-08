namespace TableSimulator.Core.Statistics;

/// <summary>Co sa nazbieralo od posledneho odberu.</summary>
public sealed record TrafficSample(IReadOnlyList<LineMinute> Lines, IReadOnlyList<BoardMinute> Boards, IReadOnlyList<LineStatusChange> StatusChanges)
{
    public bool IsEmpty => Lines.Count == 0 && Boards.Count == 0 && StatusChanges.Count == 0;
}
