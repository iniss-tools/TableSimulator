using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

/// <summary>
/// Stav presmerovania liniek servera INISS2 pre tlacidlo na stranke Linky - vzdy uspech, ina instalacia alebo chyba
/// citania = presmerovanie nie je k dispozicii.
/// </summary>
public sealed record GetInissRedirectQuery : IQuery<InissRedirectDto>;

public sealed class GetInissRedirectQueryHandler(ISettingsStore settings) : IQueryHandler<GetInissRedirectQuery, InissRedirectDto>
{
    private static readonly InissRedirectDto Unavailable = new(false, false);

    public Task<Result<InissRedirectDto>> Handle(GetInissRedirectQuery query, CancellationToken cancellationToken)
    {
        if (InissStations.Iniss2Of(settings.Current) is null || InissStations.Read(settings.Current) is not { IsSuccess: true } station)
            return Task.FromResult(Result.Success(Unavailable));

        var lines = station.Value.Lines;
        var available = lines.Any(l => l is { Active: true, Supported: true, CanRedirect: true });
        return Task.FromResult(Result.Success(new InissRedirectDto(available, lines.Any(l => l.OriginalPort is not null))));
    }
}
