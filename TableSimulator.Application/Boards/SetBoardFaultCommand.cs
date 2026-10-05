using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Zapne alebo vypne predstieranu poruchu tabule.</summary>
public sealed record SetBoardFaultCommand(string Id, BoardFaultDto Fault) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
