namespace TableSimulator.Contracts.Settings;

/// <summary>Oznamenia o vypadkoch liniek mimo prehliadaca (webhook, e-mail); heslo SMTP len ako priznak.</summary>
/// <param name="Enabled">oznamenia su zapnute</param>
/// <param name="DelaySeconds">vypadok sa oznami, az ked trva aspon tolko sekund (kratky restart INISSu nie)</param>
/// <param name="Recovery">oznamit aj navrat linky</param>
/// <param name="Language">jazyk textu oznameni: <c>sk</c> alebo <c>cs</c></param>
/// <param name="WebhookUrl">adresa, kam ide POST s JSON (pole <c>text</c> citaju aj Teams a Slack); prazdna = bez webhooku</param>
/// <param name="EmailTo">prijemcovia e-mailu oddeleni ciarkou alebo bodkociarkou; prazdne = bez e-mailu</param>
/// <param name="EmailFrom">odosielatel</param>
/// <param name="SmtpHost">server SMTP</param>
/// <param name="SmtpPort">port SMTP (587 = STARTTLS)</param>
/// <param name="SmtpSsl">sifrovat spojenie (STARTTLS)</param>
/// <param name="SmtpUser">pouzivatel SMTP; prazdny = bez prihlasenia</param>
/// <param name="SmtpPasswordSet">heslo SMTP je ulozene</param>
public sealed record AlertSettingsDto(
    bool Enabled,
    int DelaySeconds,
    bool Recovery,
    string Language,
    string WebhookUrl,
    string EmailTo,
    string EmailFrom,
    string SmtpHost,
    int SmtpPort,
    bool SmtpSsl,
    string SmtpUser,
    bool SmtpPasswordSet);
