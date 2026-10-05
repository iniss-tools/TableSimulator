using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Https;

namespace TableSimulator.Application.Https;

/// <summary>Certifikaty HTTPS servera - implementuje server.</summary>
public interface IHttpsCertificates
{
    /// <summary>Stav HTTPS a certifikatu.</summary>
    HttpsStatusDto Status();

    /// <summary>
    /// Certifikat (DER), ktoremu maju zariadenia doverovat - certifikacna autorita simulatora alebo vlastny certifikat;
    /// null, ak HTTPS nebezi.
    /// </summary>
    HttpsCertificateFile? TrustedCertificate();

    /// <summary>Overi, ci sa vlastny certifikat da pouzit (subor, heslo, sukromny kluc).</summary>
    Result Check(string certificate, string key, string password);
}

/// <summary>Certifikat na stiahnutie.</summary>
/// <param name="Content">certifikat v tvare DER</param>
/// <param name="FileName">meno suboru</param>
/// <param name="Authority">je to certifikacna autorita (zariadenie ho ma nainstalovat ako doveryhodnu autoritu)</param>
public sealed record HttpsCertificateFile(byte[] Content, string FileName, bool Authority);
