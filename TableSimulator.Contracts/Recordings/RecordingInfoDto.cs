namespace TableSimulator.Contracts.Recordings;

/// <summary>Zaznam komunikacie v zozname.</summary>
/// <param name="Duration">dlzka [s]</param>
/// <param name="Frames">pocet kusov prevadzky od INISSu</param>
public sealed record RecordingInfoDto(string Name, long Size, DateTimeOffset Created, double Duration, int[] Lines, int Frames, string? Note);
