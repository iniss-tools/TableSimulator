using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TableSimulator.Application.Https;
using TableSimulator.Infrastructure.Https;

namespace TableSimulator.Tests.Https;

/// <summary>Certifikacna autorita simulatora, certifikat servera a vlastny certifikat zo suboru.</summary>
[TestClass]
public sealed class LocalCertificatesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private static readonly CertificateNames Names =
        new(["localhost", "simulator"], [IPAddress.Loopback, IPAddress.Parse("192.168.1.20")]);

    private readonly List<string> _folders = [];

    private string Folder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        _folders.Add(folder);
        return folder;
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var folder in _folders)
            Directory.Delete(folder, true);
    }

    [TestMethod]
    public void CertifikatServera_PodpisanyAutoritouPreMenaAAdresy()
    {
        using var authority = LocalCertificates.CreateAuthority(Now);
        using var server = LocalCertificates.CreateServer(authority, Names, Now);

        Assert.IsTrue(authority.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority);
        Assert.IsFalse(server.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority);
        Assert.AreEqual("1.3.6.1.5.5.7.3.1", server.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single().EnhancedKeyUsages[0].Value);
        Assert.IsTrue(server.HasPrivateKey);
        Assert.IsTrue(Names.CoveredBy(server));
        Assert.IsTrue(LocalCertificates.IssuedBy(server, authority));
        Assert.IsLessThanOrEqualTo(LocalCertificates.ServerValidity, server.NotAfter.ToUniversalTime() - Now.UtcDateTime);
        CollectionAssert.AreEqual(new[] { "localhost", "simulator", "127.0.0.1", "192.168.1.20" }, Names.All.ToArray());
    }

    [TestMethod]
    public void CertifikatServera_InaAutoritaAleboNoveMeno_Nepouzitelny()
    {
        using var authority = LocalCertificates.CreateAuthority(Now);
        using var other = LocalCertificates.CreateAuthority(Now);
        using var server = LocalCertificates.CreateServer(authority, Names, Now);

        Assert.IsFalse(LocalCertificates.IssuedBy(server, other));
        Assert.IsFalse(new CertificateNames(["localhost"], [IPAddress.Parse("10.0.0.7")]).CoveredBy(server));
    }

    [TestMethod]
    public void Odtlacok_Sha256SDvojbodkami()
    {
        using var authority = LocalCertificates.CreateAuthority(Now);

        var thumbprint = LocalCertificates.Thumbprint(authority);

        Assert.AreEqual(32 * 3 - 1, thumbprint.Length);
        Assert.AreEqual(Convert.ToHexString(authority.GetCertHash(HashAlgorithmName.SHA256)), thumbprint.Replace(":", "", StringComparison.Ordinal));
    }

    /// <summary>Vlastny certifikat ako subory: PEM s klucom zvlast a PFX s heslom.</summary>
    private (string Pem, string Key, string Pfx) OwnFiles(string password)
    {
        var folder = Folder();
        using var authority = LocalCertificates.CreateAuthority(Now);
        using var server = LocalCertificates.CreateServer(authority, Names, Now);
        var pem = Path.Combine(folder, "server.crt");
        var key = Path.Combine(folder, "server.key");
        var pfx = Path.Combine(folder, "server.pfx");
        File.WriteAllText(pem, server.ExportCertificatePem());
        File.WriteAllText(key, server.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem());
        File.WriteAllBytes(pfx, server.Export(X509ContentType.Pkcs12, password));
        return (pem, key, pfx);
    }

    [TestMethod]
    public void VlastnyCertifikat_PemAPfxSHeslom()
    {
        var (pem, key, pfx) = OwnFiles("tajne");

        using var fromPem = HttpsCertificates.Load(pem, key, "");
        using var fromPfx = HttpsCertificates.Load(pfx, "", "tajne");

        Assert.IsTrue(fromPem.HasPrivateKey);
        Assert.IsTrue(fromPfx.HasPrivateKey);
        Assert.AreEqual(fromPem.Thumbprint, fromPfx.Thumbprint);
    }

    [TestMethod]
    public void VlastnyCertifikat_ChybySKodom()
    {
        var (pem, _, pfx) = OwnFiles("tajne");

        string Code(string certificate, string key, string password) =>
            Assert.ThrowsExactly<CertificateLoadException>(() => HttpsCertificates.Load(certificate, key, password)).Error.Code;

        Assert.AreEqual(HttpsErrors.CertificateMissing, Code(pem + ".nie", "", ""));
        Assert.AreEqual(HttpsErrors.CertificateMissing, Code(pem, pem + ".nie", ""));
        Assert.AreEqual(HttpsErrors.CertificateKey, Code(pem, "", ""));
        Assert.AreEqual(HttpsErrors.CertificateInvalid, Code(pfx, "", "zle"));
    }
}
