using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

public sealed class GetRecordingsQueryHandler(RecordingService recordings) : IQueryHandler<GetRecordingsQuery, RecordingsDto>
{
    public Task<Result<RecordingsDto>> Handle(GetRecordingsQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(recordings.List()));
}
