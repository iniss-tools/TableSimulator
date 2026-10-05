using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Auth;

/// <summary>Odhlasi pouzivatela.</summary>
public sealed record LogoutCommand : ICommand;
