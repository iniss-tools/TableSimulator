namespace TableSimulator.Core.Archive;

/// <summary>Co pribudlo v sledovanom archive hlaseni.</summary>
/// <param name="Reset">sleduje sa iny subor (novy den) alebo sa subor skratil - predchadzajuce zaznamy neplatia</param>
/// <param name="Added">nove zaznamy v poradi suboru (pri <paramref name="Reset" /> vsetky zaznamy suboru)</param>
public sealed record ReportTailUpdate(bool Reset, IReadOnlyList<ReportArchiveEntry> Added)
{
    public bool IsEmpty => !Reset && Added.Count == 0;
}
