using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

public sealed class ImportInissStationCommandHandler(ISettingsStore settings, StationImporter importer)
    : ICommandHandler<ImportInissStationCommand, StationImportedDto>
{
    public async Task<Result<StationImportedDto>> Handle(ImportInissStationCommand command, CancellationToken cancellationToken)
    {
        var station = InissStations.Read(settings.Current);
        if (station.IsFailure)
            return Result.Failure<StationImportedDto>(station.Error);
        return Result.Success(await importer.ImportAsync(InissStations.ToRequest(station.Value), cancellationToken).ConfigureAwait(false));
    }
}
