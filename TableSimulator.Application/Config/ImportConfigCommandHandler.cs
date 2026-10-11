using System.Text.Json;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Config;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Config;

public sealed class ImportConfigCommandHandler(Simulator simulator, TransportManager transport, IFaceRepository faces, FaceHeaders headers, ILiveNotifier live)
    : ICommandHandler<ImportConfigCommand, ConfigImportedDto>
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

            config = await ImportFacesAsync(config, true, cancellationToken).ConfigureAwait(false);
            simulator.Load(config);
            await transport.SyncAsync().ConfigureAwait(false);
            return Result.Success(new ConfigImportedDto(config.Lines.Count, config.Boards.Count, true));
        }

        config = await ImportFacesAsync(config, false, cancellationToken).ConfigureAwait(false);
        var before = simulator.Read(s => (Lines: s.Lines.Count(), Boards: s.Boards.Count()));
        simulator.AddMissing(config);
        var after = simulator.Read(s => (Lines: s.Lines.Count(), Boards: s.Boards.Count()));
        if (after.Lines > before.Lines)
            await transport.SyncAsync().ConfigureAwait(false);
        return Result.Success(new ConfigImportedDto(after.Lines - before.Lines, after.Boards - before.Boards, false));
    }

    /// <summary>
    /// Vlastne predlohy ciel zo suboru do databazy (pri nahradeni prepisu rovnomenne, pri doplneni len chybajuce)
    /// a hlavicky tabul zo starsich suborov na predlohy.
    /// </summary>
    private async Task<SimulatorConfig> ImportFacesAsync(SimulatorConfig config, bool replace, CancellationToken cancellationToken)
    {
        var saved = false;
        foreach (var face in config.Faces.Where(f => BoardFaces.IsValidId(f.Id) && BoardFaces.FindBuiltIn(f.Id) is null).DistinctBy(f => f.Id))
        {
            if (!replace && await faces.FindAsync(face.Id, cancellationToken).ConfigureAwait(false) is not null)
                continue;
            var name = string.IsNullOrWhiteSpace(face.Name) ? face.Id : face.Name.Trim();
            if (name.Length > 100)
                name = name[..100];
            await faces.SaveAsync(new FaceRecord(face.Id, name, BoardFaces.Serialize(face with { Name = name })), cancellationToken).ConfigureAwait(false);
            saved = true;
        }

        var (absorbed, changed) = await headers.AbsorbAsync(config with { Faces = [] }, cancellationToken).ConfigureAwait(false);
        if (saved || changed)
            live.Publish(new FacesMessage());
        return absorbed;
    }
}
