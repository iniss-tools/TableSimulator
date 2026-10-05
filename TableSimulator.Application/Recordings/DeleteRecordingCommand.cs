using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Recordings;

/// <summary>Odstrani zaznam (nie prave nahravany).</summary>
public sealed record DeleteRecordingCommand(string Name) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Name;
}
