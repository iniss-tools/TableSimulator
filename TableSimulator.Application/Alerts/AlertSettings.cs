namespace TableSimulator.Application.Alerts;

/// <summary>
/// Oznamenia o vypadkoch liniek mimo prehliadaca - webhook a e-mail (hodi sa pri behu ako sluzba, ked nikto nema
/// otvoreny web). Vypadok = INISS sa odpojil (TCP, rura) alebo linka nebezi.
/// </summary>
public sealed record AlertSettings
{
    /// <summary>Najdlhsie cakanie na oznamenie vypadku [s].</summary>
    public const int MaxDelaySeconds = 3600;

    public bool Enabled { get; init; }

    /// <summary>Vypadok sa oznami, az ked trva aspon tolko sekund.</summary>
    public int DelaySeconds { get; init; } = 30;

    /// <summary>Oznamit aj navrat linky (len po oznamenom vypadku).</summary>
    public bool Recovery { get; init; } = true;

    /// <summary>Jazyk textu: <c>sk</c> alebo <c>cs</c>.</summary>
    public string Language { get; init; } = "sk";

    public string WebhookUrl { get; init; } = "";

    /// <summary>Prijemcovia oddeleni ciarkou alebo bodkociarkou.</summary>
    public string EmailTo { get; init; } = "";

    public string EmailFrom { get; init; } = "";
    public string SmtpHost { get; init; } = "";
    public int SmtpPort { get; init; } = 587;
    public bool SmtpSsl { get; init; } = true;
    public string SmtpUser { get; init; } = "";

    /// <summary>Heslo SMTP zasifrovane ochranou dat servera (<see cref="Settings.ISecretProtector" />).</summary>
    public string SmtpPassword { get; init; } = "";

    /// <summary>Prijemcovia e-mailu.</summary>
    public IReadOnlyList<string> Recipients =>
        EmailTo.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
