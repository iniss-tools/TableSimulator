namespace TableSimulator.Contracts.Recordings;

/// <summary>Hustota sprav INISSu v zazname - pocet sprav v rovnako dlhych usekoch od zaciatku po koniec.</summary>
/// <param name="Duration">dlzka zaznamu [s]</param>
/// <param name="Counts">pocet sprav v kazdom useku</param>
public sealed record RecordingActivityDto(double Duration, IReadOnlyList<int> Counts);
