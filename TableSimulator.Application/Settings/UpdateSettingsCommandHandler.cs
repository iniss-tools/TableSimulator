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
            MonitorFolder = (request.MonitorFolder ?? "").Trim(),
            RequireLogin = request.RequireLogin,
            VirtualFolders = VirtualFolders(request.VirtualFolders)
        };
        // port, pristup zo siete, prihlasovanie a priecinok vzhladov server pouzije az po restarte
        var restart = updated.HttpPort != current.HttpPort || updated.LoginRequired != current.LoginRequired
                      || updated.AllowNetwork != current.AllowNetwork || updated.DesignsFolder != current.DesignsFolder;
        settings.Save(updated);
        live.Publish(new StructureMessage());
        return Task.FromResult(Result.Success(new SettingsUpdatedDto(restart)));
    }

    // prazdne riadky a opakovania sa vynechaju
    private static string[] VirtualFolders(IReadOnlyList<string>? folders) =>
        (folders ?? []).Select(f => f.Trim()).Where(f => f.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
