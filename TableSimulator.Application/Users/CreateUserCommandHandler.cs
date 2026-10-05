using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

public sealed class CreateUserCommandHandler(IUserService users) : ICommandHandler<CreateUserCommand>
{
    public Task<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken) =>
        users.CreateAsync(command.Request.UserName.Trim(), DisplayName(command.Request.DisplayName, command.Request.UserName), command.Request.Role, command.Request.Password, cancellationToken);

    // prazdne meno pre zobrazenie = prihlasovacie meno
    private static string DisplayName(string? displayName, string userName) =>
        string.IsNullOrWhiteSpace(displayName) ? userName.Trim() : displayName.Trim();
}
