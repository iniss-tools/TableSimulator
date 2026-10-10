using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

public sealed class SetupCommandHandler(IAuthService auth, RoleCatalog roles) : ICommandHandler<SetupCommand, MeDto>
{
    public async Task<Result<MeDto>> Handle(SetupCommand command, CancellationToken cancellationToken)
    {
        if (!auth.LoginRequired)
            return Result.Failure<MeDto>(AuthErrors.NotRequired());
        if (await auth.HasAdminAsync(cancellationToken).ConfigureAwait(false))
            return Result.Failure<MeDto>(AuthErrors.SetupDone());
        // na servere dostupnom zo siete by si spravcu mohol zalozit hocikto, kto pride prvy
        if (!auth.IsLocalRequest)
            return Result.Failure<MeDto>(AuthErrors.SetupLocalOnly());

        var displayName = string.IsNullOrWhiteSpace(command.DisplayName) ? command.UserName.Trim() : command.DisplayName.Trim();
        var created = await auth.CreateFirstAdminAsync(command.UserName.Trim(), displayName, command.Password, cancellationToken).ConfigureAwait(false);
        return created.IsSuccess ? Result.Success(MeFactory.Create(roles, auth, created.Value)) : Result.Failure<MeDto>(created.Error);
    }
}
