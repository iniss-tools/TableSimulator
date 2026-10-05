using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Application.Users;

/// <summary>Nastavi pouzivatelovi nove heslo (spravca).</summary>
public sealed record SetUserPasswordCommand(string UserName, SetPasswordRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => UserName;
}
