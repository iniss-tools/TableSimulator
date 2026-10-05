using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

public sealed class UpdateUserCommandHandler(IUserService users) : ICommandHandler<UpdateUserCommand>
{
    public Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken) =>
        users.UpdateAsync(command.UserName, DisplayName(command.Request.DisplayName, command.UserName), command.Request.Role, command.Request.Disabled, cancellationToken);

    // prazdne meno pre zobrazenie = prihlasovacie meno
    private static string DisplayName(string? displayName, string userName) =>
        string.IsNullOrWhiteSpace(displayName) ? userName.Trim() : displayName.Trim();
}
