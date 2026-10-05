namespace TableSimulator.Contracts.Recordings;

/// <summary>Zoznam zaznamov so stavom zaznamu a prehravania.</summary>
public sealed record RecordingsDto(RecordingInfoDto[] Items, RecorderStatusDto Recorder, ReplayStatusDto? Replay);
