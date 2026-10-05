using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Station;

public sealed class ImportStationCommandHandler(Simulator simulator, TransportManager transport) : ICommandHandler<ImportStationCommand, StationImportedDto>
{
    public async Task<Result<StationImportedDto>> Handle(ImportStationCommand command, CancellationToken cancellationToken)
    {
        var result = simulator.ImportStation(RequestMapper.ToImport(command.Request));
        await transport.SyncAsync().ConfigureAwait(false);
        return Result.Success(DtoMapper.ToDto(result));
    }
}
