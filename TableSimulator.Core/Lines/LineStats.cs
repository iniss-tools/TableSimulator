namespace TableSimulator.Core.Lines;

/// <summary>
/// Pocitadla linky.
/// </summary>
public sealed class LineStats
{
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
    public long Frames { get; set; }
    public long BadFrames { get; set; }
    public long Discarded { get; set; }
    public long Connections { get; set; }

    /// <summary>Stav pocitadiel - prehratie naprazdno ich na konci vrati.</summary>
    internal (long BytesIn, long BytesOut, long Frames, long BadFrames, long Discarded, long Connections) Save() =>
        (BytesIn, BytesOut, Frames, BadFrames, Discarded, Connections);

    internal void Restore((long BytesIn, long BytesOut, long Frames, long BadFrames, long Discarded, long Connections) saved) =>
        (BytesIn, BytesOut, Frames, BadFrames, Discarded, Connections) = saved;
}
