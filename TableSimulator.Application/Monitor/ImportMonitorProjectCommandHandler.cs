using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Lines;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Monitor;
using TableSimulator.Core.Monitor;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Monitor;

public sealed class ImportMonitorProjectCommandHandler(Simulator simulator, ISettingsStore settings)
    : ICommandHandler<ImportMonitorProjectCommand, MonitorImportedDto>
{
    public Task<Result<MonitorImportedDto>> Handle(ImportMonitorProjectCommand command, CancellationToken cancellationToken)
    {
        var monitor = new MonitorFolder(settings.Current.MonitorFolder);
        var entries = monitor.Project(command.Project);
        if (entries.Count == 0)
            return Task.FromResult(Result.Failure<MonitorImportedDto>(MonitorErrors.ProjectNotFound(command.Project)));
        // tabule dostanu vyrobcu samoucenia linky
        if (simulator.Read(s => s.FindLine(command.Line)?.Manufacturer) is not { } manufacturer)
            return Task.FromResult(Result.Failure<MonitorImportedDto>(LineErrors.NotFound(command.Line)));

        var count = 0;
        foreach (var entry in entries)
        {
            if (monitor.Panel(entry.ConfigFile) is not { } template)
                continue;
            simulator.SetBoard(new BoardConfig(command.Line, entry.Addresses[0], manufacturer)
            {
                Name = string.IsNullOrWhiteSpace(entry.Location) ? template.Name : entry.Location,
                Profile = template.Profile,
                SecondAddress = entry.Addresses.Count > 1 ? entry.Addresses[1] : null
            });
            count++;
        }

        return Task.FromResult(Result.Success(new MonitorImportedDto(count)));
    }
}
