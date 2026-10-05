using RecordingFileFormat = TableSimulator.Core.Recording.RecordingFile;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Recordings;

public sealed class GetRecordingFileQueryHandler(RecordingService recordings) : IQueryHandler<GetRecordingFileQuery, RecordingFile>
{
    public Task<Result<RecordingFile>> Handle(GetRecordingFileQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(recordings.Exists(query.Name)
            ? Result.Success(new RecordingFile(recordings.PathFor(query.Name)!, query.Name + RecordingFileFormat.Extension))
            : Result.Failure<RecordingFile>(RecordingErrors.NotFound(query.Name)));
}
