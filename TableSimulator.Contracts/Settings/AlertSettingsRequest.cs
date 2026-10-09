namespace TableSimulator.Contracts.Settings;

/// <summary>Zmena oznameni o vypadkoch liniek (cast PUT /api/settings, skusobne oznamenie).</summary>
public sealed record AlertSettingsRequest
{
    public bool Enabled { get; init; }
    public int DelaySeconds { get; init; } = 30;
    public bool Recovery { get; init; } = true;
    public string Language { get; init; } = "sk";
    public string WebhookUrl { get; init; } = "";
    public string EmailTo { get; init; } = "";
    public string EmailFrom { get; init; } = "";
    public string SmtpHost { get; init; } = "";
    public int SmtpPort { get; init; } = 587;
    public bool SmtpSsl { get; init; } = true;
    public string SmtpUser { get; init; } = "";

    /// <summary>Heslo SMTP; null = nechat ulozene, prazdne = zmazat.</summary>
    public string? SmtpPassword { get; init; }
}
