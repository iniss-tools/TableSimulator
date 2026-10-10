using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Application.Scenarios;

public sealed class GetFaultScenariosQueryHandler(IFaultScenarioRepository scenarios, ScenarioRunner runner)
    : IQueryHandler<GetFaultScenariosQuery, IReadOnlyList<FaultScenarioDto>>
{
    public async Task<Result<IReadOnlyList<FaultScenarioDto>>> Handle(GetFaultScenariosQuery query, CancellationToken cancellationToken)
    {
        var records = await scenarios.ListAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<FaultScenarioDto>>(records.Select(r => ScenarioMapping.ToDto(r, runner.Status(r.Id))).ToList());
    }
}

public sealed class CreateFaultScenarioCommandHandler(IFaultScenarioRepository scenarios, ILiveNotifier live)
    : ICommandHandler<CreateFaultScenarioCommand, FaultScenarioDto>
{
    public async Task<Result<FaultScenarioDto>> Handle(CreateFaultScenarioCommand command, CancellationToken cancellationToken)
    {
        var name = command.Request.Name.Trim();
        var definition = ScenarioMapping.Serialize(command.Request with { Name = name });
        var id = await scenarios.AddAsync(name, definition, cancellationToken).ConfigureAwait(false);
        live.Publish(new ScenariosMessage());
        return Result.Success(ScenarioMapping.ToDto(new FaultScenarioRecord(id, name, definition), null));
    }
}

public sealed class SetFaultScenarioCommandHandler(IFaultScenarioRepository scenarios, ScenarioRunner runner, ILiveNotifier live)
    : ICommandHandler<SetFaultScenarioCommand>
{
    public async Task<Result> Handle(SetFaultScenarioCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request with { Name = command.Request.Name.Trim() };
        if (!await scenarios.UpdateAsync(new FaultScenarioRecord(command.Id, request.Name, ScenarioMapping.Serialize(request)), cancellationToken)
                .ConfigureAwait(false))
            return Result.Failure(ScenarioErrors.NotFound(command.Id));
        await runner.ReplaceAsync(command.Id, ScenarioMapping.ToCore(request)).ConfigureAwait(false);
        live.Publish(new ScenariosMessage());
        return Result.Success();
    }
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

public sealed class StopFaultScenarioCommandHandler(ScenarioRunner runner) : ICommandHandler<StopFaultScenarioCommand>
{
    public async Task<Result> Handle(StopFaultScenarioCommand command, CancellationToken cancellationToken)
    {
        await runner.StopAsync(command.Id).ConfigureAwait(false);
        return Result.Success();
    }
}
