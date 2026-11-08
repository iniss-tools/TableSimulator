namespace TableSimulator.Mcp.Results;

/// <summary>Rozhlas vzdialeneho INISSu (linka triedy 128) - fronty, hlasenia a ich prerusenie.</summary>
public sealed record RemoteAudioResult(IReadOnlyList<RemoteAudioDevice> Devices);
