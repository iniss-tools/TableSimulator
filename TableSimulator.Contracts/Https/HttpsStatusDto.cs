namespace TableSimulator.Contracts.Https;

/// <summary>Stav HTTPS servera (GET /api/https).</summary>
/// <param name="Enabled">HTTPS bezi - port bol nastaveny pri starte servera</param>
/// <param name="Port">port HTTPS, na ktorom server pocuva</param>
/// <param name="Redirect">pristup zo siete cez HTTP sa presmeruje na HTTPS</param>
/// <param name="Source">odkial je certifikat</param>
/// <param name="Subject">subjekt certifikatu servera</param>
/// <param name="Issuer">vydavatel certifikatu servera</param>
/// <param name="Thumbprint">odtlacok SHA-256 certifikatu, ktoremu maju zariadenia doverovat (autorita simulatora alebo vlastny certifikat)</param>
/// <param name="NotAfter">koniec platnosti certifikatu servera</param>
/// <param name="Names">mena a adresy, pre ktore certifikat plati</param>
/// <param name="Error">kod chyby vlastneho certifikatu (nedal sa nacitat - server pouziva certifikat simulatora); null = bez chyby</param>
/// <param name="ErrorDetail">anglicky popis chyby</param>
public sealed record HttpsStatusDto(
    bool Enabled,
    int Port,
    bool Redirect,
    HttpsSource Source,
    string Subject,
    string Issuer,
    string Thumbprint,
    DateTimeOffset? NotAfter,
    IReadOnlyList<string> Names,
    string? Error,
    string? ErrorDetail);
