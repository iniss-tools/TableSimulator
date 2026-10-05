using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

/// <summary>Ukonci zaznam.</summary>
public sealed record StopRecordingCommand : ICommand<RecorderStatusDto>;
