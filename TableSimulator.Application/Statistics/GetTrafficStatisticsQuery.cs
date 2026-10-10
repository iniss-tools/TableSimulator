using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Statistics;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Statistics;

namespace TableSimulator.Application.Statistics;

/// <summary>Statistika komunikacie za obdobie [<paramref name="From" />, <paramref name="To" />); linka null = vsetky.</summary>
public sealed record GetTrafficStatisticsQuery(DateTimeOffset From, DateTimeOffset To, int? Line) : IQuery<TrafficStatisticsDto>;

public sealed class GetTrafficStatisticsQueryValidator : AbstractValidator<GetTrafficStatisticsQuery>
{
    public const string Range = "statistics.range";
    public const string Line = "statistics.line";

    public GetTrafficStatisticsQueryValidator()
    {
        RuleFor(q => q.To).GreaterThan(q => q.From).WithErrorCode(Range).WithMessage("The end must be after the start.");
        RuleFor(q => q).Must(q => q.To - q.From <= TimeSpan.FromDays(TrafficRetention.MaxSpanDays)).WithErrorCode(Range)
            .WithMessage($"The period must not be longer than {TrafficRetention.MaxSpanDays} days.");
        RuleFor(q => q.Line).InclusiveBetween(1, 100).When(q => q.Line is not null).WithErrorCode(Line);
    }
}

public sealed class GetTrafficStatisticsQueryHandler(ITrafficStatisticsRepository statistics)
    : IQueryHandler<GetTrafficStatisticsQuery, TrafficStatisticsDto>
{
    public async Task<Result<TrafficStatisticsDto>> Handle(GetTrafficStatisticsQuery query, CancellationToken cancellationToken)
    {
        var bucket = TrafficRetention.BucketFor(query.To - query.From);
        // zaciatok na hranici intervalu - intervaly su nasobky dlzky od 1. 1. 1970 UTC
        var from = TrafficMinutes.Of(query.From) / bucket * bucket;
        var to = TrafficMinutes.Of(query.To - TimeSpan.FromMilliseconds(1)) + 1;
        var data = await statistics.QueryAsync(from, to, bucket, query.Line, cancellationToken).ConfigureAwait(false);

        static DateTimeOffset Time(long minute) => TrafficMinutes.Start(minute);
        return Result.Success(new TrafficStatisticsDto(Time(from), query.To, bucket,
            data.Lines.OrderBy(l => l.Line).ThenBy(l => l.Minute)
                .Select(l => new LineTrafficPointDto(l.Line, Time(l.Minute), l.Ok, l.BadChecksum, l.NoBoard, l.Noise, l.Other, l.Replies, l.Faults,
                    l.BytesIn, l.BytesOut, l.Connections, l.Disconnections)).ToList(),
            data.Boards.OrderBy(b => b.Line).ThenBy(b => b.Address).ThenBy(b => b.Minute)
                .Select(b => new BoardTrafficPointDto(b.Line, b.Address, Time(b.Minute), b.Frames, b.Acks, b.Naks, b.Unanswered, b.MaxGapMs)).ToList(),
            data.StatusChanges.OrderBy(s => s.TimeMs).ThenBy(s => s.Id)
                .Select(s => new LineStatusChangeDto(s.Line, DateTimeOffset.FromUnixTimeMilliseconds(s.TimeMs),
                    DtoMapper.ToDto(SimulatorConfigJson.Parse<LineStatus>(s.Status)), s.Detail)).ToList(),
            TrafficRetention.Days));
    }
}
