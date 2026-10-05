namespace TableSimulator.Contracts.Boards;

/// <summary>Porucha tabule (aj telo POST /api/boards/{id}/fault).</summary>
/// <param name="Kind">druh</param>
/// <param name="DelayMs">oneskorenie odpovede pri <see cref="FaultKind.Delay" /> [ms]</param>
/// <param name="Status">stavove bity pri <see cref="FaultKind.Status" /></param>
public sealed record BoardFaultDto(FaultKind Kind, int DelayMs = 0, int Status = 0);
