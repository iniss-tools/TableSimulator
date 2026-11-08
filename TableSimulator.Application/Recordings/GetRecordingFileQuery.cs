using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Recordings;

/// <summary>Subor zaznamu na stiahnutie.</summary>
public sealed record GetRecordingFileQuery(string Name) : IQuery<RecordingFile>;
