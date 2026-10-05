using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

/// <summary>Zalozi prveho spravcu a prihlasi ho.</summary>
public sealed record SetupCommand(string UserName, string DisplayName, string Password) : ICommand<MeDto>, IAuditable
{
    string IAuditable.AuditTarget => UserName;
}
