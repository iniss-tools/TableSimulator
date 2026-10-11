using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Faces;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Config;

public sealed class ExportConfigQueryHandler(Simulator simulator, IFaceRepository faces) : IQueryHandler<ExportConfigQuery, ConfigFile>
{
    public const string FileName = "tablesimulator-linky.json";

    public async Task<Result<ConfigFile>> Handle(ExportConfigQuery query, CancellationToken cancellationToken)
    {
        var config = simulator.ToConfig();
        // vlastne predlohy ciel idu so subormi - vstavane ma kazdy server
        var used = config.Boards.Select(b => b.Profile.Face).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var custom = (await faces.ListAsync(cancellationToken).ConfigureAwait(false))
            .Where(f => used.Contains(f.Id))
            .Select(BoardFaces.Deserialize)
            .ToList();
        return Result.Success(new ConfigFile(SimulatorConfigJson.Serialize(config with { Faces = custom }), FileName));
    }
}
