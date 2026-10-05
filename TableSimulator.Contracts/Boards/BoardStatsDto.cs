namespace TableSimulator.Contracts.Boards;

/// <summary>Pocitadla komunikacie tabule.</summary>
public sealed record BoardStatsDto(long Frames, long Acks, long Naks, long Unanswered, DateTimeOffset? LastFrameAt);
