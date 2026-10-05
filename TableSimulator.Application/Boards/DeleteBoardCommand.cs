using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Boards;

/// <summary>Odstrani tabulu.</summary>
public sealed record DeleteBoardCommand(string Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
