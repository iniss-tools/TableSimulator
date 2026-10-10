using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.State;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.State;

public sealed class GetStateQueryHandler(Simulator simulator, ISettingsStore settings, DatabaseInfo database) : IQueryHandler<GetStateQuery, StateDto>
{
    public Task<Result<StateDto>> Handle(GetStateQuery query, CancellationToken cancellationToken)
    {
        var current = settings.Current;
        var state = simulator.Read(s => new StateDto(
            s.Lines.Select(l => DtoMapper.ToDto(l, current.ConnectHost)).ToArray(),
            s.Boards.Select(DtoMapper.ToDto).ToArray(),
            Simulator.SupportedClasses.ToArray(),
            DtoMapper.Manufacturers,
            DtoMapper.ToDto(current, database)));
        return Task.FromResult(Result.Success(state));
    }
}
