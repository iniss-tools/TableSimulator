using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

public sealed class GetInissStationQueryHandler(ISettingsStore settings) : IQueryHandler<GetInissStationQuery, InissStationDto>
{
    public Task<Result<InissStationDto>> Handle(GetInissStationQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(InissStations.Read(settings.Current));
}
