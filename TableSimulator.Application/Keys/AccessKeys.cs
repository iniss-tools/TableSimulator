using System.Security.Cryptography;
using System.Text;
using TableSimulator.Contracts.Keys;

namespace TableSimulator.Application.Keys;

/// <summary>Tvorba a overenie tajnej hodnoty pristupovych klucov.</summary>
public static class AccessKeys
{
    /// <summary>Druh v databaze - zobrazovacie zariadenie.</summary>
    public const string DeviceKind = "device";

    /// <summary>Druh v databaze - program.</summary>
    public const string ApiKind = "api";

    /// <summary>Zaciatok kazdeho kluca (rozlisi ho od hesla aj v zaznamoch).</summary>
    public const string SecretPrefix = "ts_";

    /// <summary>Kolko znakov z kluca sa uklada na rozlisenie.</summary>
    public const int PrefixLength = 10;

    /// <summary>Ako casto sa zapisuje posledne pouzitie (nie pri kazdej poziadavke).</summary>
    public static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

    /// <summary>Novy kluc: 32 nahodnych bajtov v base64url za <see cref="SecretPrefix" />.</summary>
    public static string NewSecret() => SecretPrefix + Base64Url(RandomNumberGenerator.GetBytes(32));

    /// <summary>Hash SHA-256 kluca (hex) - podla neho sa kluc hlada v databaze.</summary>
    public static string Hash(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    public static string KindOf(AccessKeyKind kind) => kind == AccessKeyKind.Device ? DeviceKind : ApiKind;

    public static AccessKeyKind KindOf(string kind) => kind == DeviceKind ? AccessKeyKind.Device : AccessKeyKind.Api;

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
