using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Archive;
using TableSimulator.Core.Archive;

namespace TableSimulator.Application.Archive;

public sealed class GetReportArchiveQueryHandler(ArchiveCatalog archive) : IQueryHandler<GetReportArchiveQuery, ReportArchivePageDto>
{
    public Task<Result<ReportArchivePageDto>> Handle(GetReportArchiveQuery query, CancellationToken cancellationToken)
    {
        ArchiveContent? content;
        try
        {
            content = archive.Read(ArchiveKind.Reports, query.Date);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure<ReportArchivePageDto>(ArchiveErrors.Unreadable(e.Message)));
        }

        if (content is null)
            return Task.FromResult(Result.Failure<ReportArchivePageDto>(ArchiveErrors.NotFound(query.Date)));

        var matching = content.Reports.Where(e => ArchiveFilter.Matches(e, query.Train, query.Text)).ToArray();
        IEnumerable<ReportArchiveEntry> ordered = query.Newest ? matching.Reverse() : matching;
        var page = ordered.Skip(query.Offset).Take(query.Limit).Select(DtoMapper.ToDto).ToArray();
        return Task.FromResult(Result.Success(new ReportArchivePageDto(query.Date, page, matching.Length, content.Skipped)));
    }
}
