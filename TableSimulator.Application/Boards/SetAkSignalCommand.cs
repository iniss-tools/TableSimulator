using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Nastavi snimac tmy, stavy a casovanie ustredne AK Signal.</summary>
public sealed record SetAkSignalCommand(string Id, AkSignalSettingsRequest Settings) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
