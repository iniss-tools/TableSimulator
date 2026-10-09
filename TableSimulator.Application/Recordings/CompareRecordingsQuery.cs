using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;
using TableSimulator.Core.Recording;
using RecordingFileFormat = TableSimulator.Core.Recording.RecordingFile;

namespace TableSimulator.Application.Recordings;

/// <summary>Porovna, co ukazovali tabule v dvoch zaznamoch.</summary>
/// <param name="A">prvy zaznam (napr. pred zmenou grafikonu)</param>
/// <param name="B">druhy zaznam (po zmene)</param>
public sealed record CompareRecordingsQuery(string A, string B) : IQuery<RecordingComparisonDto>;

public sealed class CompareRecordingsQueryValidator : AbstractValidator<CompareRecordingsQuery>
{
    public CompareRecordingsQueryValidator()
    {
        RuleFor(q => q.A).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
        RuleFor(q => q.B).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
    }
}

public sealed class CompareRecordingsQueryHandler(RecordingService recordings) : IQueryHandler<CompareRecordingsQuery, RecordingComparisonDto>
{
    public async Task<Result<RecordingComparisonDto>> Handle(CompareRecordingsQuery query, CancellationToken cancellationToken)
    {
        foreach (var name in new[] { query.A, query.B })
        {
            if (!recordings.Exists(name))
                return Result.Failure<RecordingComparisonDto>(RecordingErrors.NotFound(name));
        }

        // prehratie dlhych zaznamov chvilu trva - mimo vlakna poziadavky
        return await Task.Run(() =>
        {
            RecordingFileFormat a, b;
            try
            {
                a = RecordingFileFormat.Load(recordings.PathFor(query.A)!);
                b = RecordingFileFormat.Load(recordings.PathFor(query.B)!);
            }
            catch (FormatException)
            {
                return Result.Failure<RecordingComparisonDto>(RecordingErrors.Format());
            }

            return Result.Success(Compare(query.A, a, query.B, b));
        }, cancellationToken).ConfigureAwait(false);
    }

    private static RecordingComparisonDto Compare(string nameA, RecordingFileFormat a, string nameB, RecordingFileFormat b)
    {
        var timelinesA = RecordingTimeline.Build(a).ToDictionary(t => t.Id, StringComparer.Ordinal);
        var timelinesB = RecordingTimeline.Build(b).ToDictionary(t => t.Id, StringComparer.Ordinal);
        var boards = timelinesA.Keys.Union(timelinesB.Keys).Order(BoardIdComparer.Instance).Select(id =>
        {
            var statesA = timelinesA.TryGetValue(id, out var ta) ? ta.States : [];
            var statesB = timelinesB.TryGetValue(id, out var tb) ? tb.States : [];
            var steps = TimelineDiff.Compare(statesA, statesB);
            var result = statesA.Count == 0 ? BoardDifference.OnlyB
                : statesB.Count == 0 ? BoardDifference.OnlyA
                : steps.All(s => s.Kind == DiffKind.Same) ? BoardDifference.Same
                : BoardDifference.Different;
            var name = ta?.Name is { Length: > 0 } n ? n : tb?.Name ?? "";
            return new BoardComparisonDto(id, name, result, statesA.Count, statesB.Count, [.. steps.Select(ToDto)]);
        }).ToList();
        return new RecordingComparisonDto(Side(nameA, a), Side(nameB, b), boards);
    }

    private static RecordingSideDto Side(string name, RecordingFileFormat recording) =>
        new(name, recording.Header.Created, recording.Duration, recording.Header.Note);

    private static ComparisonStepDto ToDto(DiffStep step) => new(step.Kind switch
    {
        DiffKind.Same => ComparisonStepKind.Same,
        DiffKind.Changed => ComparisonStepKind.Changed,
        DiffKind.Removed => ComparisonStepKind.Removed,
        _ => ComparisonStepKind.Added
    }, ToDto(step.A), ToDto(step.B));

    private static BoardStateDto? ToDto(BoardState? state) => state is null ? null : new BoardStateDto(state.Time, state.Settled, state.Rows);
}
