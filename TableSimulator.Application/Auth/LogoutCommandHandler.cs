using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Auth;

public sealed class LogoutCommandHandler(IAuthService auth) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        await auth.SignOutAsync().ConfigureAwait(false);
        return Result.Success();
    }
}
