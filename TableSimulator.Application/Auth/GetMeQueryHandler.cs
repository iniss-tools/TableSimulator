using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

public sealed class GetMeQueryHandler(IAuthService auth, ICurrentUser user) : IQueryHandler<GetMeQuery, MeDto>
{
    public async Task<Result<MeDto>> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        var setup = auth.LoginRequired && !await auth.HasUsersAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(user.IsAuthenticated
            ? MeFactory.Create(auth.LoginRequired, setup, user.UserName, user.DisplayName, user.Role)
            : MeFactory.Create(auth.LoginRequired, setup, null, null, null));
    }
}
