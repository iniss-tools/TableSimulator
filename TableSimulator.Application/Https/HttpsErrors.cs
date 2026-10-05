using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Https;

/// <summary>Chyby a kody chyb HTTPS.</summary>
public static class HttpsErrors
{
    /// <summary>Subor certifikatu alebo kluca neexistuje.</summary>
    public const string CertificateMissing = "settings.httpsCertificateMissing";

    /// <summary>Subor sa neda precitat ako certifikat (tvar, heslo).</summary>
    public const string CertificateInvalid = "settings.httpsCertificateInvalid";

    /// <summary>Certifikat nema sukromny kluc.</summary>
    public const string CertificateKey = "settings.httpsCertificateKey";

    public static Error Disabled() => Error.NotFound("https.disabled", "HTTPS is not enabled on this server.");
}
