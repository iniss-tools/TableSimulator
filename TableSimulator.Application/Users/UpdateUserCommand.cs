using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Application.Users;

/// <summary>Zmeni meno, rolu a vypnutie pouzivatela.</summary>
public sealed record UpdateUserCommand(string UserName, UpdateUserRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => UserName;
}
