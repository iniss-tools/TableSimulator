using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Application.Designs;

public sealed class GetDesignsQueryHandler(DesignCatalog designs) : IQueryHandler<GetDesignsQuery, DesignInfoDto[]>
{
    public Task<Result<DesignInfoDto[]>> Handle(GetDesignsQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(designs.List()));
}
