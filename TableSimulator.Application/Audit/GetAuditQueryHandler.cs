using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Audit;

namespace TableSimulator.Application.Audit;

public sealed class GetAuditQueryHandler(IAuditLog log) : IQueryHandler<GetAuditQuery, AuditPageDto>
{
    public async Task<Result<AuditPageDto>> Handle(GetAuditQuery query, CancellationToken cancellationToken)
    {
        var entries = await log.ListAsync(query.Before, Empty(query.User), Empty(query.Action), query.Limit, cancellationToken).ConfigureAwait(false);
        var page = entries.Take(query.Limit)
            .Select(e => new AuditEntryDto(e.Id, DateTime.SpecifyKind(e.Time, DateTimeKind.Utc), e.User, e.Action, e.Target, e.Success, e.Error, e.Details, e.Address))
            .ToArray();
        return Result.Success(new AuditPageDto(page, entries.Count > query.Limit));
    }

    private static string? Empty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
