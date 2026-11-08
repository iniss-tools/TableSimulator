using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Scenarios;

/// <summary>Spusti scenar porucha od zaciatku.</summary>
public sealed record StartFaultScenarioCommand(int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}

public sealed class StartFaultScenarioCommandHandler(IFaultScenarioRepository scenarios, ScenarioRunner runner) : ICommandHandler<StartFaultScenarioCommand>
{
    public async Task<Result> Handle(StartFaultScenarioCommand command, CancellationToken cancellationToken)
    {
        if (await scenarios.FindAsync(command.Id, cancellationToken).ConfigureAwait(false) is not { } record)
            return Result.Failure(ScenarioErrors.NotFound(command.Id));
        await runner.StartAsync(command.Id, ScenarioMapping.ToCore(ScenarioMapping.Deserialize(record.Definition))).ConfigureAwait(false);
        return Result.Success();
    }
}
