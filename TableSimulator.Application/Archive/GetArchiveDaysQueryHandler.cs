using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Archive;
using TableSimulator.Core.Archive;

namespace TableSimulator.Application.Archive;

public sealed class GetArchiveDaysQueryHandler(ArchiveCatalog archive) : IQueryHandler<GetArchiveDaysQuery, ArchiveDaysDto>
{
    public Task<Result<ArchiveDaysDto>> Handle(GetArchiveDaysQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new ArchiveDaysDto(
            archive.Days(ArchiveKind.Tables).Select(d => d.Date).ToArray(),
            archive.Days(ArchiveKind.Reports).Select(d => d.Date).ToArray())));
}
