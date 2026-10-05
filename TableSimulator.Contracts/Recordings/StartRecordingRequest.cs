namespace TableSimulator.Contracts.Recordings;

/// <summary>Zaciatok zaznamu (POST /api/recordings/start).</summary>
/// <param name="Note">poznamka k zaznamu</param>
public sealed record StartRecordingRequest(string? Note);
