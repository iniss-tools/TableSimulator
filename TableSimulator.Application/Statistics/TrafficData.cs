namespace TableSimulator.Application.Statistics;

/// <summary>Co dotaz na statistiku vrati z databazy - intervaly s komunikaciou a zmeny stavu liniek.</summary>
public sealed record TrafficData(IReadOnlyList<LineTrafficRecord> Lines, IReadOnlyList<BoardTrafficRecord> Boards, IReadOnlyList<LineStatusRecord> StatusChanges);
