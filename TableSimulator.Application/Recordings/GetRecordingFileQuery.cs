using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Recordings;

/// <summary>Subor zaznamu na stiahnutie.</summary>
public sealed record GetRecordingFileQuery(string Name) : IQuery<RecordingFile>;
