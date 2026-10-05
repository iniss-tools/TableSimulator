using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Recordings;

/// <summary>Odstrani zaznam (nie prave nahravany).</summary>
public sealed record DeleteRecordingCommand(string Name) : ICommand;
