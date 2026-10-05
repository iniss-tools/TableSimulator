using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Settings;

public sealed class UpdateSettingsCommandHandler(ISettingsStore settings, ILiveNotifier live) : ICommandHandler<UpdateSettingsCommand, SettingsUpdatedDto>
{
    public Task<Result<SettingsUpdatedDto>> Handle(UpdateSettingsCommand command, CancellationToken cancellationToken)
    {
        var current = settings.Current;
        var request = command.Request;
        var updated = current with
        {
            HttpPort = request.HttpPort,
            AllowNetwork = request.AllowNetwork,
            FontsFolder = (request.FontsFolder ?? "").Trim(),
            DesignsFolder = (request.DesignsFolder ?? "").Trim(),
            ConnectHost = (request.ConnectHost ?? "").Trim(),
            MonitorFolder = (request.MonitorFolder ?? "").Trim()
        };
        // port, pristup zo siete a priecinok vzhladov server pouzije az po restarte
        var restart = updated.HttpPort != current.HttpPort || updated.AllowNetwork != current.AllowNetwork || updated.DesignsFolder != current.DesignsFolder;
        settings.Save(updated);
        live.Publish(new StructureMessage());
        return Task.FromResult(Result.Success(new SettingsUpdatedDto(restart)));
    }
}
