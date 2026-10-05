using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Application.Users;

/// <summary>Zalozi pouzivatela.</summary>
public sealed record CreateUserCommand(CreateUserRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Request.UserName;
}
