namespace TableSimulator.Contracts.Frames;

/// <summary>Odpoved tabule na vlozenu spravu.</summary>
/// <param name="Hex">bajty odpovede ako hex</param>
/// <param name="DelayMs">oneskorenie odpovede predstieranou poruchou [ms]</param>
public sealed record FrameReplyDto(string Hex, int DelayMs);
