using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Https;

namespace TableSimulator.Application.Https;

public sealed class GetHttpsStatusQueryHandler(IHttpsCertificates certificates) : IQueryHandler<GetHttpsStatusQuery, HttpsStatusDto>
{
    public Task<Result<HttpsStatusDto>> Handle(GetHttpsStatusQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(certificates.Status()));
}
