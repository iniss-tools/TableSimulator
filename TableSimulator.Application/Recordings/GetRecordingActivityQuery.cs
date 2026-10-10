using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

/// <summary>Hustota sprav INISSu v zazname po <paramref name="Buckets" /> usekoch (casova os prehravania).</summary>
public sealed record GetRecordingActivityQuery(string Name, int Buckets) : IQuery<RecordingActivityDto>;

public sealed class GetRecordingActivityQueryValidator : AbstractValidator<GetRecordingActivityQuery>
{
    public GetRecordingActivityQueryValidator()
    {
        RuleFor(q => q.Name).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
        RuleFor(q => q.Buckets).InclusiveBetween(1, 2000).WithErrorCode(RecordingErrors.Buckets);
    }
}

public sealed class GetRecordingActivityQueryHandler(RecordingService recordings) : IQueryHandler<GetRecordingActivityQuery, RecordingActivityDto>
{
    public async Task<Result<RecordingActivityDto>> Handle(GetRecordingActivityQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return await recordings.ActivityAsync(query.Name, query.Buckets).ConfigureAwait(false) is { } activity
                ? Result.Success(activity)
                : Result.Failure<RecordingActivityDto>(RecordingErrors.NotFound(query.Name));
        }
        catch (FormatException)
        {
            return Result.Failure<RecordingActivityDto>(RecordingErrors.Format());
        }
    }
}
