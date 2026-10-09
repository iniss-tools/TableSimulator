using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Core.Recording;
using RecordingFileFormat = TableSimulator.Core.Recording.RecordingFile;

namespace TableSimulator.Application.Recordings;

/// <summary>Tabula zo zaznamu tak, ako vyzerala v case <paramref name="Time" /> (s od zaciatku zaznamu).</summary>
public sealed record GetRecordingBoardQuery(string Name, string Board, double Time) : IQuery<BoardDto>;

public sealed class GetRecordingBoardQueryValidator : AbstractValidator<GetRecordingBoardQuery>
{
    public GetRecordingBoardQueryValidator()
    {
        RuleFor(q => q.Name).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
        RuleFor(q => q.Time).GreaterThanOrEqualTo(0).WithErrorCode(RecordingErrors.Time);
    }
}

public sealed class GetRecordingBoardQueryHandler(RecordingService recordings) : IQueryHandler<GetRecordingBoardQuery, BoardDto>
{
    public async Task<Result<BoardDto>> Handle(GetRecordingBoardQuery query, CancellationToken cancellationToken)
    {
        if (!recordings.Exists(query.Name))
            return Result.Failure<BoardDto>(RecordingErrors.NotFound(query.Name));

        return await Task.Run(() =>
        {
            RecordingFileFormat recording;
            try
            {
                recording = RecordingFileFormat.Load(recordings.PathFor(query.Name)!);
            }
            catch (FormatException)
            {
                return Result.Failure<BoardDto>(RecordingErrors.Format());
            }

            var simulator = RecordingTimeline.ReplayUntil(recording, query.Time);
            return simulator.Read(s => s.FindBoard(query.Board)) is { } board
                ? Result.Success(simulator.Read(_ => DtoMapper.ToDto(board)))
                : Result.Failure<BoardDto>(RecordingErrors.BoardNotFound(query.Board));
        }, cancellationToken).ConfigureAwait(false);
    }
}
