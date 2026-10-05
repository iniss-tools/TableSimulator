using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

public sealed class DeleteUserCommandHandler(IUserService users) : ICommandHandler<DeleteUserCommand>
{
    public Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken) =>
        users.DeleteAsync(command.UserName, cancellationToken);
}
