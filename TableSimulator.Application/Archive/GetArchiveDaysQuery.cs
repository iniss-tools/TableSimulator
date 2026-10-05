using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Archive;

namespace TableSimulator.Application.Archive;

/// <summary>Dni dennych archivov v priecinkoch logov INISSu.</summary>
public sealed record GetArchiveDaysQuery : IQuery<ArchiveDaysDto>;
