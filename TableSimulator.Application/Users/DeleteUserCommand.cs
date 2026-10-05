using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Users;

/// <summary>Odstrani pouzivatela.</summary>
public sealed record DeleteUserCommand(string UserName) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => UserName;
}
