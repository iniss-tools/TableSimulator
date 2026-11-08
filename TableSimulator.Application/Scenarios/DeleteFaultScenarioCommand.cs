using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Scenarios;

/// <summary>Odstrani scenar porucha (beziaci zastavi).</summary>
public sealed record DeleteFaultScenarioCommand(int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}

public sealed class DeleteFaultScenarioCommandHandler(IFaultScenarioRepository scenarios, ScenarioRunner runner, ILiveNotifier live)
    : ICommandHandler<DeleteFaultScenarioCommand>
{
    public async Task<Result> Handle(DeleteFaultScenarioCommand command, CancellationToken cancellationToken)
    {
        if (!await scenarios.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false))
            return Result.Failure(ScenarioErrors.NotFound(command.Id));
        await runner.StopAsync(command.Id).ConfigureAwait(false);
        live.Publish(new ScenariosMessage());
        return Result.Success();
    }
}
