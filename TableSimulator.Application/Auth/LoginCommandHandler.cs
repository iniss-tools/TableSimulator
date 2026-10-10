using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

public sealed class LoginCommandHandler(IAuthService auth, RoleCatalog roles) : ICommandHandler<LoginCommand, MeDto>
{
    public async Task<Result<MeDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (!auth.LoginRequired)
            return Result.Failure<MeDto>(AuthErrors.NotRequired());

        var (outcome, user) = await auth.SignInAsync(command.UserName.Trim(), command.Password, command.Remember, cancellationToken).ConfigureAwait(false);
        return outcome switch
        {
            SignInOutcome.Success when user is not null => Result.Success(MeFactory.Create(roles, auth, user)),
            SignInOutcome.LockedOut => Result.Failure<MeDto>(AuthErrors.LockedOut()),
            SignInOutcome.Disabled => Result.Failure<MeDto>(AuthErrors.Disabled()),
            SignInOutcome.TooManyAttempts => Result.Failure<MeDto>(AuthErrors.TooManyAttempts()),
            _ => Result.Failure<MeDto>(AuthErrors.InvalidCredentials())
        };
    }
}
