using System.Runtime.Versioning;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging.Abstractions;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Kluce ochrany dat (podpis prihlasenia, sifrovane hodnoty nastaveni) su v priecinku nastaveni zasifrovane DPAPI.
/// Nove sa sifruju pre pocitac - precita ich server spusteny pouzivatelom aj sluzba Windows; starsie kluce sifrovane
/// pre pouzivatela prevedie <see cref="ToLocalMachine" /> (pri instalacii sluzby, spustenej tym pouzivatelom).
/// </summary>
public static class KeyRingProtection
{
    /// <summary>Kluce ochrany dat v priecinku, vo Windows sifrovane DPAPI pre pocitac.</summary>
    public static IDataProtectionBuilder PersistKeys(this IDataProtectionBuilder builder, string keysFolder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.PersistKeysToFileSystem(new DirectoryInfo(keysFolder));
        if (OperatingSystem.IsWindows())
            builder.ProtectKeysWithDpapi(protectToLocalMachine: true);
        return builder;
    }

    /// <summary>
    /// Prevedie kluce sifrovane DPAPI (pre pouzivatela aj pocitac) na sifrovanie pre pocitac. Vrati pocet prevedenych
    /// klucov; kluc, ktory sa neda rozsifrovat (iny pouzivatel), ostane bez zmeny a vrati sa v <paramref name="failed" />.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static int ToLocalMachine(string keysFolder, out IReadOnlyList<string> failed)
    {
        var errors = new List<string>();
        failed = errors;
        if (!Directory.Exists(keysFolder))
            return 0;

        var decryptor = new DpapiXmlDecryptor(null);
        var encryptor = new DpapiXmlEncryptor(protectToLocalMachine: true, NullLoggerFactory.Instance);
        var converted = 0;
        foreach (var file in Directory.EnumerateFiles(keysFolder, "key-*.xml"))
        {
            var document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
            // element je v mennom priestore ochrany dat
            var secrets = document.Descendants().Where(e => e.Name.LocalName == "encryptedSecret")
                .Where(e => (string?)e.Attribute("decryptorType") is { } type && type.Contains(nameof(DpapiXmlDecryptor), StringComparison.Ordinal))
                .ToList();
            if (secrets.Count == 0)
                continue;
            try
            {
                foreach (var secret in secrets)
                {
                    var plain = decryptor.Decrypt(secret.Elements().Single());
                    var encrypted = encryptor.Encrypt(plain);
                    secret.ReplaceNodes(encrypted.EncryptedElement);
                    secret.SetAttributeValue("decryptorType", encrypted.DecryptorType.AssemblyQualifiedName);
                }
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                errors.Add(Path.GetFileName(file));
                continue;
            }

            document.Save(file);
            converted++;
        }

        return converted;
    }
}
