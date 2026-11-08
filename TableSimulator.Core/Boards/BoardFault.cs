namespace TableSimulator.Core.Boards;

/// <summary>
/// Porucha tabule.
/// </summary>
/// <param name="Kind">druh</param>
/// <param name="DelayMs">oneskorenie odpovede pri <see cref="FaultKind.Delay" /></param>
/// <param name="Status">stavove bity pri <see cref="FaultKind.Status" /> (napr. 0x08 porucha izolacie, 0x40 napajanie)</param>
public sealed record BoardFault(FaultKind Kind, int DelayMs = 0, int Status = 0)
{
    /// <summary>Bez poruchy.</summary>
    public static BoardFault None { get; } = new(FaultKind.None);
}
