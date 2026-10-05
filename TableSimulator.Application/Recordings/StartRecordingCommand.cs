using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

/// <summary>Zacne zaznam prevadzky vsetkych liniek.</summary>
public sealed record StartRecordingCommand(string? Note) : ICommand<RecorderStatusDto>;
