using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

public sealed class SetUserPasswordCommandHandler(IUserService users) : ICommandHandler<SetUserPasswordCommand>
{
    public Task<Result> Handle(SetUserPasswordCommand command, CancellationToken cancellationToken) =>
        users.SetPasswordAsync(command.UserName, command.Request.Password, cancellationToken);
}
