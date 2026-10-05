using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Https;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Https;

namespace TableSimulator.Infrastructure.Https;

/// <summary>
/// Certifikat HTTPS servera. Bez vlastneho certifikatu ho vyda certifikacna autorita simulatora (subory v priecinku
/// <c>https</c> nastaveni, sukromne kluce sifrovane ochranou dat servera) - pri novej adrese pocitaca alebo mesiac
/// pred koncom platnosti vyda novy, autorita ostava. Vlastny certifikat (.pfx, .p12, PEM) sa nacita znova, ked sa
/// subor alebo nastavenie zmeni. Ak sa vlastny certifikat nacitat neda, server pouzije certifikat simulatora a chybu
/// ukaze v stave.
/// </summary>
public sealed partial class HttpsCertificates(
    AppPaths paths,
    ISettingsStore settings,
    ServerEndpoints endpoints,
    ISecretProtector secrets,
    IDataProtectionProvider protection,
    TimeProvider time,
    ILogger<HttpsCertificates> logger) : IHttpsCertificates
{
    // ako casto sa pri spojeni kontroluju subory a adresy pocitaca
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    // novy certifikat servera vydat mesiac pred koncom platnosti
    private static readonly TimeSpan RenewBefore = TimeSpan.FromDays(30);

    private readonly Lock _lock = new();
    private readonly IDataProtector _protector = protection.CreateProtector("TableSimulator.Https");
    private X509Certificate2? _authority;
    private X509Certificate2? _server;
    private HttpsSource _source;
    private FileVersion? _file;
    private Error? _fileError;
    private DateTimeOffset _checked;
    private AppSettings? _checkedSettings;

    /// <summary>Priecinok certifikatov simulatora.</summary>
    public string Folder => Path.Combine(paths.DataFolder, "https");

    private string AuthorityKeyFile => Path.Combine(Folder, "authority.key");
    private string AuthorityFile => Path.Combine(Folder, "authority.crt");
    private string ServerKeyFile => Path.Combine(Folder, "server.key");

    /// <summary>Certifikat pre nove spojenie (Kestrel); pri zmene suboru, nastavenia alebo adries sa obnovi.</summary>
    public X509Certificate2 Current
    {
        get
        {
            lock (_lock)
            {
                Refresh(false);
                return _server!;
            }
        }
    }

    /// <summary>Pripravi certifikat pri starte servera (chyby sa ukazu hned v logu, nie az pri prvom spojeni).</summary>
    public void Initialize()
    {
        lock (_lock)
            Refresh(true);
        string thumbprint;
        lock (_lock)
            thumbprint = LocalCertificates.Thumbprint(TrustedLocked());
        CertificateReady(logger, _source, _server!.Subject, _server.NotAfter, thumbprint);
    }

    /// <inheritdoc />
    public HttpsStatusDto Status()
    {
        if (!endpoints.HttpsEnabled)
            return new HttpsStatusDto(false, 0, false, HttpsSource.Generated, "", "", "", null, [], null, null);
        lock (_lock)
        {
            Refresh(false);
            var server = _server!;
            var names = server.Extensions.OfType<X509SubjectAlternativeNameExtension>().FirstOrDefault() is { } alternative
                ? [.. alternative.EnumerateDnsNames(), .. alternative.EnumerateIPAddresses().Select(a => a.ToString())]
                : Array.Empty<string>();
            return new HttpsStatusDto(true, endpoints.HttpsPort, endpoints.HttpsRedirect, _source, server.Subject, server.Issuer,
                LocalCertificates.Thumbprint(TrustedLocked()), new DateTimeOffset(server.NotAfter.ToUniversalTime(), TimeSpan.Zero), names,
                _fileError?.Code, _fileError?.Description);
        }
    }

    /// <inheritdoc />
    public HttpsCertificateFile? TrustedCertificate()
    {
        if (!endpoints.HttpsEnabled)
            return null;
        lock (_lock)
        {
            Refresh(false);
            return _source == HttpsSource.Generated
                ? new HttpsCertificateFile(_authority!.Export(X509ContentType.Cert), "TableSimulator-CA.crt", true)
                : new HttpsCertificateFile(_server!.Export(X509ContentType.Cert), "TableSimulator.crt", false);
        }
    }

    /// <inheritdoc />
    public Result Check(string certificate, string key, string password)
    {
        try
        {
            using var loaded = Load(certificate, key, password);
            return Result.Success();
        }
        catch (CertificateLoadException e)
        {
            return Result.Failure(e.Error);
        }
    }

    /// <summary>
    /// Nacita vlastny certifikat: .pfx a .p12 ako PKCS#12 (s heslom), ostatne ako PEM - kluc v subore certifikatu
    /// alebo v <paramref name="key" />, s heslom zasifrovany.
    /// </summary>
    /// <exception cref="CertificateLoadException">subor chyba, neda sa precitat alebo certifikat nema kluc</exception>
    public static X509Certificate2 Load(string certificate, string key, string password)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(password);
        foreach (var file in new[] { certificate, key }.Where(f => f.Length > 0))
            if (!File.Exists(file))
                throw new CertificateLoadException(Error.Problem(HttpsErrors.CertificateMissing, $"The file {file} does not exist."));

        X509Certificate2 loaded;
        try
        {
            var extension = Path.GetExtension(certificate).ToUpperInvariant();
            if (extension is ".PFX" or ".P12")
            {
                loaded = X509CertificateLoader.LoadPkcs12FromFile(certificate, password);
            }
            else
            {
                var keyFile = key.Length > 0 ? key : null;
                if (keyFile is null && !File.ReadAllText(certificate).Contains("PRIVATE KEY", StringComparison.Ordinal))
                    throw new CertificateLoadException(Error.Problem(HttpsErrors.CertificateKey, "The certificate file has no private key - set the key file."));
                using var pem = password.Length > 0
                    ? X509Certificate2.CreateFromEncryptedPemFile(certificate, password, keyFile)
                    : X509Certificate2.CreateFromPemFile(certificate, keyFile);
                loaded = LocalCertificates.Reload(pem);
            }
        }
        catch (Exception e) when (e is CryptographicException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new CertificateLoadException(Error.Problem(HttpsErrors.CertificateInvalid, $"The certificate cannot be read: {e.Message}"));
        }

        if (!loaded.HasPrivateKey)
        {
            loaded.Dispose();
            throw new CertificateLoadException(Error.Problem(HttpsErrors.CertificateKey, "The certificate has no private key."));
        }

        return loaded;
    }

    private X509Certificate2 TrustedLocked() => _source == HttpsSource.Generated ? _authority! : _server!;

    private void Refresh(bool force)
    {
        var now = time.GetUtcNow();
        var current = settings.Current;
        if (!force && _server is not null && ReferenceEquals(current, _checkedSettings) && now - _checked < CheckInterval)
            return;
        _checked = now;
        _checkedSettings = current;

        if (current.HttpsCertificate.Length > 0)
        {
            var version = FileVersion.Of(current);
            if (version == _file && (_source == HttpsSource.File || _fileError is not null) && _server is not null)
            {
                if (_source == HttpsSource.Generated)
                    EnsureGenerated(now, current);
                return;
            }

            _file = version;
            try
            {
                Replace(Load(current.HttpsCertificate, current.HttpsKey, secrets.Unprotect(current.HttpsPassword)), HttpsSource.File);
                _fileError = null;
                return;
            }
            catch (CertificateLoadException e)
            {
                _fileError = e.Error;
                FileFailed(logger, current.HttpsCertificate, e.Error.Description);
            }
        }
        else
        {
            _file = null;
            _fileError = null;
        }

        EnsureGenerated(now, current);
    }

    private void EnsureGenerated(DateTimeOffset now, AppSettings current)
    {
        _authority ??= LoadOrCreateAuthority(now);
        var names = CertificateNames.OfThisComputer(current.AllowNetwork, current.ConnectHost);
        if (_source == HttpsSource.Generated && _server is not null && Usable(_server, names, now))
            return;

        if (ReadProtected(ServerKeyFile) is { } stored)
        {
            if (Usable(stored, names, now))
            {
                Replace(stored, HttpsSource.Generated);
                return;
            }

            stored.Dispose();
        }

        var created = LocalCertificates.CreateServer(_authority, names, now);
        WriteProtected(ServerKeyFile, created);
        Replace(created, HttpsSource.Generated);
        ServerIssued(logger, names.All, created.NotAfter);
    }

    private bool Usable(X509Certificate2 certificate, CertificateNames names, DateTimeOffset now) =>
        certificate.NotAfter.ToUniversalTime() > (now + RenewBefore).UtcDateTime && names.CoveredBy(certificate)
                                                  && LocalCertificates.IssuedBy(certificate, _authority!);

    private X509Certificate2 LoadOrCreateAuthority(DateTimeOffset now)
    {
        if (ReadProtected(AuthorityKeyFile) is { } stored)
        {
            if (stored.NotAfter.ToUniversalTime() > (now + TimeSpan.FromDays(365)).UtcDateTime)
                return stored;
            stored.Dispose();
        }
        else if (File.Exists(AuthorityKeyFile))
        {
            // napr. iny pouzivatel Windows - kluce ochrany dat sa nedaju precitat
            AuthorityUnreadable(logger, AuthorityKeyFile);
        }

        var authority = LocalCertificates.CreateAuthority(now);
        WriteProtected(AuthorityKeyFile, authority);
        File.WriteAllBytes(AuthorityFile, authority.Export(X509ContentType.Cert));
        // certifikaty servera starej autority neplatia
        File.Delete(ServerKeyFile);
        AuthorityCreated(logger, AuthorityFile, LocalCertificates.Thumbprint(authority));
        return authority;
    }

    // stary certifikat sa nezahodi - moze ho este pouzivat rozbehnute spojenie
    private void Replace(X509Certificate2 certificate, HttpsSource source)
    {
        _server = certificate;
        _source = source;
    }

    private X509Certificate2? ReadProtected(string file)
    {
        if (!File.Exists(file))
            return null;
        try
        {
            return X509CertificateLoader.LoadPkcs12(_protector.Unprotect(File.ReadAllBytes(file)), null);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private void WriteProtected(string file, X509Certificate2 certificate)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllBytes(file, _protector.Protect(certificate.Export(X509ContentType.Pkcs12)));
    }

    /// <summary>Verzia vlastneho certifikatu - subory, ich cas zmeny a heslo.</summary>
    private sealed record FileVersion(string Certificate, string Key, string Password, DateTime CertificateTime, DateTime KeyTime)
    {
        public static FileVersion Of(AppSettings settings) =>
            new(settings.HttpsCertificate, settings.HttpsKey, settings.HttpsPassword, Time(settings.HttpsCertificate), Time(settings.HttpsKey));

        private static DateTime Time(string file) => file.Length > 0 && File.Exists(file) ? File.GetLastWriteTimeUtc(file) : default;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "HTTPS: certifikat {Source} {Subject} plati do {NotAfter:yyyy-MM-dd}, odtlacok doveryhodneho certifikatu {Thumbprint}")]
    private static partial void CertificateReady(ILogger logger, HttpsSource source, string subject, DateTime notAfter, string thumbprint);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTPS: vlastny certifikat {File} sa neda pouzit ({Reason}) - server pouzije certifikat simulatora")]
    private static partial void FileFailed(ILogger logger, string file, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "HTTPS: novy certifikat servera pre {Names}, plati do {NotAfter:yyyy-MM-dd}")]
    private static partial void ServerIssued(ILogger logger, IReadOnlyList<string> names, DateTime notAfter);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HTTPS: nova certifikacna autorita simulatora {File} (odtlacok {Thumbprint}) - zariadenia ju musia nainstalovat")]
    private static partial void AuthorityCreated(ILogger logger, string file, string thumbprint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HTTPS: certifikacnu autoritu {File} sa neda precitat (iny pouzivatel alebo pocitac?) - vytvori sa nova")]
    private static partial void AuthorityUnreadable(ILogger logger, string file);
}

/// <summary>Vlastny certifikat sa neda pouzit.</summary>
public sealed class CertificateLoadException(Error error) : Exception(error?.Description)
{
    /// <summary>Chyba s kodom pre obsluhu.</summary>
    public Error Error { get; } = error ?? throw new ArgumentNullException(nameof(error));
}
