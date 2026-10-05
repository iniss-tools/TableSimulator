using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

/// <summary>Prihlasi pouzivatela.</summary>
public sealed record LoginCommand(string UserName, string Password, bool Remember) : ICommand<MeDto>;
