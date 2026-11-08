namespace TableSimulator.Core.Simulation;

/// <summary>
/// Odpoved INISSu - posle sa po <see cref="Delay" />.
/// </summary>
public sealed record LineReply(byte[] Bytes, TimeSpan Delay);
