using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Application.Server;

public sealed class GetServerUpdateQueryHandler(IServerUpdates updates) : IQueryHandler<GetServerUpdateQuery, ServerUpdateDto>
{
    public Task<Result<ServerUpdateDto>> Handle(GetServerUpdateQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(updates.Status));
}
