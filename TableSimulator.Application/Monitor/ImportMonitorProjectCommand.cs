using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Monitor;

namespace TableSimulator.Application.Monitor;

/// <summary>Kazdy simulator projektu ELEN Monitora sa stane tabulou na linke <paramref name="Line" />.</summary>
public sealed record ImportMonitorProjectCommand(string Project, int Line) : ICommand<MonitorImportedDto>, IAuditable
{
    string IAuditable.AuditTarget => Project;
}
