using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Boards;

/// <summary>Zmaze obsah tabule.</summary>
public sealed record ClearBoardCommand(string Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
