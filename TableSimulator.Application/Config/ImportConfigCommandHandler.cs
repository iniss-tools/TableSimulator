using System.Text.Json;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Config;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Config;

public sealed class ImportConfigCommandHandler(Simulator simulator, TransportManager transport) : ICommandHandler<ImportConfigCommand, ConfigImportedDto>
{
    public async Task<Result<ConfigImportedDto>> Handle(ImportConfigCommand command, CancellationToken cancellationToken)
    {
        SimulatorConfig config;
        try
        {
            config = SimulatorConfigJson.Deserialize(command.Content);
        }
        catch (JsonException e)
        {
            return Result.Failure<ConfigImportedDto>(ConfigErrors.Invalid(e.Message));
        }

        if (command.Replace)
        {
            // najprv na skusku - neplatna linka nesmie nechat simulator naplneny len do polovice
            try
            {
                new Simulator().Load(config);
            }
            catch (ArgumentException e)
            {
                return Result.Failure<ConfigImportedDto>(ConfigErrors.Invalid(e.Message));
            }

            simulator.Load(config);
            await transport.SyncAsync().ConfigureAwait(false);
            return Result.Success(new ConfigImportedDto(config.Lines.Count, config.Boards.Count, true));
        }

        var before = simulator.Read(s => (Lines: s.Lines.Count(), Boards: s.Boards.Count()));
        simulator.AddMissing(config);
        var after = simulator.Read(s => (Lines: s.Lines.Count(), Boards: s.Boards.Count()));
        if (after.Lines > before.Lines)
            await transport.SyncAsync().ConfigureAwait(false);
        return Result.Success(new ConfigImportedDto(after.Lines - before.Lines, after.Boards - before.Boards, false));
    }
}
