using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Application.Users;

/// <summary>Zmeni heslo prihlaseneho pouzivatela.</summary>
public sealed record ChangePasswordCommand(ChangePasswordRequest Request) : ICommand;
