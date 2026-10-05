using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Application.Users;

public sealed class GetUsersQueryHandler(IUserService users) : IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken) =>
        Result.Success(await users.ListAsync(cancellationToken).ConfigureAwait(false));
}
