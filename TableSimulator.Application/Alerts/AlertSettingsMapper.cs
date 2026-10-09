using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Alerts;

/// <summary>Nastavenie oznameni a kontrakty.</summary>
public static class AlertSettingsMapper
{
    public static AlertSettingsDto ToDto(AlertSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new AlertSettingsDto(settings.Enabled, settings.DelaySeconds, settings.Recovery, settings.Language, settings.WebhookUrl, settings.EmailTo,
            settings.EmailFrom, settings.SmtpHost, settings.SmtpPort, settings.SmtpSsl, settings.SmtpUser, settings.SmtpPassword.Length > 0);
    }

    /// <summary>Nove nastavenie z poziadavky; heslo null = ulozene, inak zasifrovane (prazdne = bez hesla).</summary>
    public static AlertSettings Apply(AlertSettings current, AlertSettingsRequest request, ISecretProtector secrets)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(secrets);
        return new AlertSettings
        {
            Enabled = request.Enabled,
            DelaySeconds = request.DelaySeconds,
            Recovery = request.Recovery,
            Language = request.Language,
            WebhookUrl = request.WebhookUrl.Trim(),
            EmailTo = request.EmailTo.Trim(),
            EmailFrom = request.EmailFrom.Trim(),
            SmtpHost = request.SmtpHost.Trim(),
            SmtpPort = request.SmtpPort,
            SmtpSsl = request.SmtpSsl,
            SmtpUser = request.SmtpUser.Trim(),
            SmtpPassword = request.SmtpPassword is null ? current.SmtpPassword : request.SmtpPassword.Length == 0 ? "" : secrets.Protect(request.SmtpPassword)
        };
    }
}
