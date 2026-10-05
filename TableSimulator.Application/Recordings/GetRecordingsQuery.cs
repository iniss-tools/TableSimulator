using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

/// <summary>Zoznam zaznamov so stavom zaznamu a prehravania.</summary>
public sealed record GetRecordingsQuery : IQuery<RecordingsDto>;
