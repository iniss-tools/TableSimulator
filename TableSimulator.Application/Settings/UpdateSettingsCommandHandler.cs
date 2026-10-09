using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Https;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Settings;

public sealed class UpdateSettingsCommandHandler(ISettingsStore settings, ILiveNotifier live, IHttpsCertificates certificates, ISecretProtector secrets)
    : ICommandHandler<UpdateSettingsCommand, SettingsUpdatedDto>
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
            VirtualFolders = VirtualFolders(request.VirtualFolders),
            InissViewFolder = (request.InissViewFolder ?? "").Trim().Trim('"'),
            InissProgram = (request.InissProgram ?? "").Trim().Trim('"'),
            InissRegistry = (request.InissRegistry ?? "").Trim(),
            InissElevated = request.InissElevated,
            HttpsPort = request.HttpsPort,
            HttpsRedirect = request.HttpsRedirect,
            HttpsCertificate = (request.HttpsCertificate ?? "").Trim().Trim('"'),
            HttpsKey = (request.HttpsKey ?? "").Trim().Trim('"'),
            // heslo sa vracia len ako priznak - bez noveho ostava ulozene
            HttpsPassword = request.HttpsPassword is null ? current.HttpsPassword : secrets.Protect(request.HttpsPassword)
        };
        // vlastny certifikat musi byt pouzitelny skor, nez sa ulozi - inak by server po restarte HTTPS nemal
        if (updated.HttpsPort > 0 && updated.HttpsCertificate.Length > 0
            && certificates.Check(updated.HttpsCertificate, updated.HttpsKey, secrets.Unprotect(updated.HttpsPassword)) is { IsFailure: true } check)
            return Task.FromResult(Result.Failure<SettingsUpdatedDto>(check.Error));

        // porty, pristup zo siete, prihlasovanie, presmerovanie na HTTPS a priecinok vzhladov server pouzije az po
        // restarte; vlastny certifikat nacita sam
        var restart = updated.HttpPort != current.HttpPort || updated.LoginRequired != current.LoginRequired
                      || updated.AllowNetwork != current.AllowNetwork || updated.DesignsFolder != current.DesignsFolder
                      || updated.HttpsPort != current.HttpsPort || updated.HttpsRedirect != current.HttpsRedirect;
        settings.Save(updated);
        live.Publish(new StructureMessage());
        return Task.FromResult(Result.Success(new SettingsUpdatedDto(restart)));
    }

    // prazdne riadky a opakovania sa vynechaju
    private static string[] VirtualFolders(IReadOnlyList<string>? folders) =>
        (folders ?? []).Select(f => f.Trim()).Where(f => f.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
