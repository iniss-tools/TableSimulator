using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Lines;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Monitor;
using TableSimulator.Core.Monitor;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Monitor;

public sealed class ImportMonitorProjectCommandHandler(Simulator simulator, ISettingsStore settings, FaceHeaders headers)
    : ICommandHandler<ImportMonitorProjectCommand, MonitorImportedDto>
{
    public async Task<Result<MonitorImportedDto>> Handle(ImportMonitorProjectCommand command, CancellationToken cancellationToken)
    {
        // tabule dostanu vyrobcu samoucenia linky - typy panelov Monitora su len tabule ELEN
        if (simulator.Read(s => s.FindLine(command.Line)?.Manufacturer) is not { } manufacturer)
            return Result.Failure<MonitorImportedDto>(LineErrors.NotFound(command.Line));
        if (ElenVariant.Find(manufacturer) is null)
            return Result.Failure<MonitorImportedDto>(MonitorErrors.NotElenLine(command.Line));

        var monitor = new MonitorFolder(settings.Current.MonitorFolder);
        var entries = monitor.Project(command.Project);
        if (entries.Count == 0)
            return Result.Failure<MonitorImportedDto>(MonitorErrors.ProjectNotFound(command.Project));

        var count = 0;
        foreach (var entry in entries)
        {
            if (monitor.Panel(entry.ConfigFile) is not { } template)
                continue;
            var board = new BoardConfig(command.Line, entry.Addresses[0], manufacturer)
            {
                Name = string.IsNullOrWhiteSpace(entry.Location) ? template.Name : entry.Location,
                Profile = template.Profile,
                SecondAddress = entry.Addresses.Count > 1 ? entry.Addresses[1] : null
            };
            // hlavicka typu panelu = predloha cela s nazvom typu panelu, spolocna pre jeho tabule
            board = await headers.AbsorbAsync(board, template.Name, "monitor-" + Path.GetFileNameWithoutExtension(template.File), cancellationToken)
                .ConfigureAwait(false);
            simulator.SetBoard(board);
            count++;
        }

        return Result.Success(new MonitorImportedDto(count));
    }
}
