using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

public sealed class ChangePasswordCommandHandler(IUserService users) : ICommandHandler<ChangePasswordCommand>
{
    public Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        users.ChangeOwnPasswordAsync(command.Request.CurrentPassword, command.Request.NewPassword, cancellationToken);
}
