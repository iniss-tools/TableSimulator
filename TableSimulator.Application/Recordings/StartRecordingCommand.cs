using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

/// <summary>Zacne zaznam prevadzky vsetkych liniek.</summary>
public sealed record StartRecordingCommand(string? Note) : ICommand<RecorderStatusDto>;
