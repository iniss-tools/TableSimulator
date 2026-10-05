using FluentValidation;

namespace TableSimulator.Application.Monitor;

public sealed class ImportMonitorProjectCommandValidator : AbstractValidator<ImportMonitorProjectCommand>
{
    public ImportMonitorProjectCommandValidator()
    {
        RuleFor(c => c.Project).NotEmpty().WithErrorCode(MonitorErrors.Project);
        RuleFor(c => c.Line).InclusiveBetween(1, 100).WithErrorCode(MonitorErrors.Line);
    }
}
