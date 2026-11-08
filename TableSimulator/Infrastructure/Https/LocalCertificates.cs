using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TableSimulator.Infrastructure.Https;

/// <summary>
/// Certifikaty simulatora: vlastna certifikacna autorita (zariadenia ju nainstaluju raz) a certifikat servera, ktory
/// podpise - ten sa da vydat znova (nova adresa, koniec platnosti) bez novej instalacie na zariadeniach. RSA 2048
/// kvoli starsim zariadeniam; certifikat servera najviac 397 dni (limit prehliadacov).
/// </summary>
public static class LocalCertificates
{
    /// <summary>Platnost certifikatu servera.</summary>
    public static readonly TimeSpan ServerValidity = TimeSpan.FromDays(397);

    private const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

    /// <summary>Nova certifikacna autorita s platnostou 10 rokov.</summary>
    public static X509Certificate2 CreateAuthority(DateTimeOffset now)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN=TableSimulator CA ({Environment.MachineName}), O=INISSTools", key, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(10));
        return Reload(certificate);
    }

    /// <summary>Novy certifikat servera pre <paramref name="names" /> podpisany autoritou <paramref name="authority" />.</summary>
    public static X509Certificate2 CreateServer(X509Certificate2 authority, CertificateNames names, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(names);
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={names.Dns[names.Dns.Count > 1 ? 1 : 0]}, O=INISSTools", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ServerAuthentication)], false));
        var alternative = new SubjectAlternativeNameBuilder();
        foreach (var dns in names.Dns)
            alternative.AddDnsName(dns);
        foreach (var address in names.Addresses)
            alternative.AddIpAddress(address);
        request.CertificateExtensions.Add(alternative.Build());
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(authority, true, false));

        var serial = RandomNumberGenerator.GetBytes(16);
        serial[0] &= 0x7F;
        var notAfter = now + ServerValidity < authority.NotAfter ? now + ServerValidity : new DateTimeOffset(authority.NotAfter);
        using var signed = request.Create(authority, now.AddDays(-1), notAfter, serial);
        using var withKey = signed.CopyWithPrivateKey(key);
        return Reload(withKey);
    }

    /// <summary>Certifikat vydala tato autorita (podpis aj vydavatel).</summary>
    public static bool IssuedBy(X509Certificate2 certificate, X509Certificate2 authority)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(authority);
        if (!certificate.IssuerName.RawData.AsSpan().SequenceEqual(authority.SubjectName.RawData))
            return false;
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(authority);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;
        return chain.Build(certificate);
    }

    /// <summary>Odtlacok SHA-256 v tvare <c>AB:CD:…</c> (ako ho ukazuju prehliadace).</summary>
    public static string Thumbprint(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return string.Join(':', certificate.GetCertHash(HashAlgorithmName.SHA256).Select(b => b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Certifikat s klucom nacitany cez PKCS#12 - kluc vytvoreny v pamati (alebo z PEM) Windows pri TLS nepouzije.
    /// Kluc ostava exportovatelny, aby sa dal ulozit.
    /// </summary>
    public static X509Certificate2 Reload(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.Exportable);
    }
}
