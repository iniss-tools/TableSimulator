using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Keys;

namespace TableSimulator.Application.Keys;

public sealed class GetAccessKeysQueryHandler(IAccessKeyRepository keys) : IQueryHandler<GetAccessKeysQuery, IReadOnlyList<AccessKeyDto>>
{
    public async Task<Result<IReadOnlyList<AccessKeyDto>>> Handle(GetAccessKeysQuery query, CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<AccessKeyDto>>((await keys.ListAsync(cancellationToken).ConfigureAwait(false)).Select(DtoMapper.ToDto).ToArray());
}
