using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Recordings;
using TableSimulator.Core.Recording;
using RecordingFileFormat = TableSimulator.Core.Recording.RecordingFile;

namespace TableSimulator.Application.Recordings;

/// <summary>Co ukazovala tabula <paramref name="Board" /> v zazname <paramref name="Name" /> - stavy ako pri porovnani zaznamov.</summary>
public sealed record GetRecordingTimelineQuery(string Name, string Board) : IQuery<BoardTimelineDto>;

public sealed class GetRecordingTimelineQueryValidator : AbstractValidator<GetRecordingTimelineQuery>
{
    public GetRecordingTimelineQueryValidator()
    {
        RuleFor(q => q.Name).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
        RuleFor(q => q.Board).NotEmpty().WithErrorCode(RecordingErrors.Board);
    }
}

public sealed class GetRecordingTimelineQueryHandler(RecordingService recordings) : IQueryHandler<GetRecordingTimelineQuery, BoardTimelineDto>
{
    public async Task<Result<BoardTimelineDto>> Handle(GetRecordingTimelineQuery query, CancellationToken cancellationToken)
    {
        if (!recordings.Exists(query.Name))
            return Result.Failure<BoardTimelineDto>(RecordingErrors.NotFound(query.Name));

        // prehratie dlhych zaznamov chvilu trva - mimo vlakna poziadavky
        return await Task.Run(() =>
        {
            RecordingFileFormat recording;
            try
            {
                recording = RecordingFileFormat.Load(recordings.PathFor(query.Name)!);
            }
            catch (FormatException)
            {
                return Result.Failure<BoardTimelineDto>(RecordingErrors.Format());
            }

            return RecordingTimeline.Build(recording).FirstOrDefault(t => t.Id == query.Board) is { } timeline
                ? Result.Success(new BoardTimelineDto(timeline.Id, timeline.Name, recording.Duration, timeline.States.Select(DtoMapper.ToDto).ToList()))
                : Result.Failure<BoardTimelineDto>(RecordingErrors.BoardNotFound(query.Board));
        }, cancellationToken).ConfigureAwait(false);
    }
}
