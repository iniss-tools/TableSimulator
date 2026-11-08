using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Scenarios;

/// <summary>Zastavi scenar porucha a vrati, co nastavil.</summary>
public sealed record StopFaultScenarioCommand(int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}

public sealed class StopFaultScenarioCommandHandler(ScenarioRunner runner) : ICommandHandler<StopFaultScenarioCommand>
{
    public async Task<Result> Handle(StopFaultScenarioCommand command, CancellationToken cancellationToken)
    {
        await runner.StopAsync(command.Id).ConfigureAwait(false);
        return Result.Success();
    }
}
