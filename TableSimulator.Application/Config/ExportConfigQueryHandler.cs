using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Config;

public sealed class ExportConfigQueryHandler(Simulator simulator) : IQueryHandler<ExportConfigQuery, ConfigFile>
{
    public const string FileName = "tablesimulator-linky.json";

    public Task<Result<ConfigFile>> Handle(ExportConfigQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new ConfigFile(SimulatorConfigJson.Serialize(simulator.ToConfig()), FileName)));
}
