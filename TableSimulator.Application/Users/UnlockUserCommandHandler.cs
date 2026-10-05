using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

public sealed class UnlockUserCommandHandler(IUserService users) : ICommandHandler<UnlockUserCommand>
{
    public Task<Result> Handle(UnlockUserCommand command, CancellationToken cancellationToken) =>
        users.UnlockAsync(command.UserName, cancellationToken);
}
