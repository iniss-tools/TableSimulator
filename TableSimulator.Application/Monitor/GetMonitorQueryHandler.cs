using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Monitor;
using TableSimulator.Core.Monitor;

namespace TableSimulator.Application.Monitor;

public sealed class GetMonitorQueryHandler(ISettingsStore settings) : IQueryHandler<GetMonitorQuery, MonitorDto>
{
    public Task<Result<MonitorDto>> Handle(GetMonitorQuery query, CancellationToken cancellationToken)
    {
        var monitor = new MonitorFolder(settings.Current.MonitorFolder);
        var dto = new MonitorDto(monitor.Exists, monitor.Root,
            monitor.Exists ? monitor.Panels().Select(DtoMapper.ToDto).ToArray() : [],
            monitor.Projects().ToArray(),
            DtoMapper.ToDto(monitor.Look()));
        return Task.FromResult(Result.Success(dto));
    }
}
