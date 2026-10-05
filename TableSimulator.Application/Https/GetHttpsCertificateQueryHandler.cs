using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Https;

public sealed class GetHttpsCertificateQueryHandler(IHttpsCertificates certificates) : IQueryHandler<GetHttpsCertificateQuery, HttpsCertificateFile>
{
    public Task<Result<HttpsCertificateFile>> Handle(GetHttpsCertificateQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(certificates.TrustedCertificate() is { } file ? Result.Success(file) : Result.Failure<HttpsCertificateFile>(HttpsErrors.Disabled()));
}
