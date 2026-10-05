namespace TableSimulator.Contracts.Lines;

/// <summary>Pocitadla linky.</summary>
public sealed record LineStatsDto(long BytesIn, long BytesOut, long Frames, long BadFrames, long Discarded, long Connections);
