using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Archive;
using TableSimulator.Core.Archive;

namespace TableSimulator.Application.Archive;

public sealed class GetTableArchiveQueryHandler(ArchiveCatalog archive) : IQueryHandler<GetTableArchiveQuery, TableArchivePageDto>
{
    public Task<Result<TableArchivePageDto>> Handle(GetTableArchiveQuery query, CancellationToken cancellationToken)
    {
        ArchiveContent? content;
        try
        {
            content = archive.Read(ArchiveKind.Tables, query.Date);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure<TableArchivePageDto>(ArchiveErrors.Unreadable(e.Message)));
        }

        if (content is null)
            return Task.FromResult(Result.Failure<TableArchivePageDto>(ArchiveErrors.NotFound(query.Date)));

        var matching = content.Tables.Where(e => ArchiveFilter.Matches(e, query.Train, query.Text)).ToArray();
        IEnumerable<TableArchiveEntry> ordered = query.Newest ? matching.Reverse() : matching;
        var page = ordered.Skip(query.Offset).Take(query.Limit).Select(DtoMapper.ToDto).ToArray();
        return Task.FromResult(Result.Success(new TableArchivePageDto(query.Date, page, matching.Length, content.Skipped)));
    }
}
