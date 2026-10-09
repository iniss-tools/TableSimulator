using System.Net.Mail;
using FluentValidation;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Alerts;

/// <summary>Kody chyb nastavenia oznameni.</summary>
public static class AlertErrors
{
    public const string Delay = "settings.alertDelay";
    public const string Webhook = "settings.alertWebhook";
    public const string Email = "settings.alertEmail";
    public const string Smtp = "settings.alertSmtp";
    public const string Language = "settings.alertLanguage";
}

/// <summary>Nastavenie oznameni: cakanie, adresa webhooku, adresy e-mailu a server SMTP.</summary>
public sealed class AlertSettingsRules : AbstractValidator<AlertSettingsRequest>
{
    public AlertSettingsRules()
    {
        RuleFor(r => r.DelaySeconds).InclusiveBetween(0, AlertSettings.MaxDelaySeconds).WithErrorCode(AlertErrors.Delay);
        RuleFor(r => r.Language).Must(l => l is "sk" or "cs").WithErrorCode(AlertErrors.Language);
        RuleFor(r => r.WebhookUrl).Must(u => string.IsNullOrWhiteSpace(u) || IsHttpUrl(u)).WithErrorCode(AlertErrors.Webhook)
            .WithMessage("The webhook must be an absolute http or https URL.");
        When(r => !string.IsNullOrWhiteSpace(r.EmailTo), () =>
        {
            RuleFor(r => r.EmailTo).Must(to => new AlertSettings { EmailTo = to }.Recipients.All(IsAddress)).WithErrorCode(AlertErrors.Email)
                .WithMessage("Every recipient must be an e-mail address.");
            RuleFor(r => r.EmailFrom).Must(IsAddress).WithErrorCode(AlertErrors.Email).WithMessage("The sender must be an e-mail address.");
            RuleFor(r => r.SmtpHost).Must(h => !string.IsNullOrWhiteSpace(h) && h.Length <= 200).WithErrorCode(AlertErrors.Smtp);
            RuleFor(r => r.SmtpPort).InclusiveBetween(1, 65535).WithErrorCode(AlertErrors.Smtp);
        });
        RuleFor(r => r.WebhookUrl.Length + r.EmailTo.Length + r.EmailFrom.Length + r.SmtpHost.Length + r.SmtpUser.Length).LessThanOrEqualTo(2000)
            .WithErrorCode(AlertErrors.Webhook);
    }

    private static bool IsHttpUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool IsAddress(string? address) => MailAddress.TryCreate(address?.Trim(), out _);
}
