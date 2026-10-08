using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

public sealed class ImportStationCommandHandler(StationImporter importer) : ICommandHandler<ImportStationCommand, StationImportedDto>
{
    public async Task<Result<StationImportedDto>> Handle(ImportStationCommand command, CancellationToken cancellationToken) =>
        Result.Success(await importer.ImportAsync(command.Request, cancellationToken).ConfigureAwait(false));
}
