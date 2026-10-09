using System.Net;
using System.Net.Mail;
using System.Text;
using TableSimulator.Application.Alerts;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Alerts;

/// <summary>Oznamenie e-mailom cez SMTP (sifrovanie STARTTLS, napr. port 587; priame SSL na porte 465 nie).</summary>
public sealed class EmailAlertChannel(ISecretProtector secrets) : IAlertChannel
{
    public string Key => "email";

    public bool IsConfigured(AlertSettings settings) => settings is { Recipients.Count: > 0, SmtpHost.Length: > 0 };

    public async Task SendAsync(AlertMessage message, AlertSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(settings);
        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.SmtpSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        if (settings.SmtpUser.Length > 0)
            client.Credentials = new NetworkCredential(settings.SmtpUser, secrets.Unprotect(settings.SmtpPassword));

        using var mail = new MailMessage
        {
            From = new MailAddress(settings.EmailFrom),
            Subject = message.Subject,
            Body = message.Text,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };
        foreach (var recipient in settings.Recipients)
            mail.To.Add(recipient);
        await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
    }
}
