using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Auth;

/// <summary>Odhlasi pouzivatela.</summary>
public sealed record LogoutCommand : ICommand;
