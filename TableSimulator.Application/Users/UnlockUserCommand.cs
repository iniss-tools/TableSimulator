using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Users;

/// <summary>Zrusi zablokovanie pouzivatela po neuspesnych pokusoch o prihlasenie.</summary>
public sealed record UnlockUserCommand(string UserName) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => UserName;
}
