namespace TableSimulator.Contracts.Recordings;

/// <summary>Porovnavany zaznam.</summary>
/// <param name="Duration">dlzka [s]</param>
public sealed record RecordingSideDto(string Name, DateTimeOffset Created, double Duration, string? Note);
