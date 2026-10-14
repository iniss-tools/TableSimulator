using System.Security.Cryptography;
using System.Text;
using TableSimulator.Application.Keys;

namespace TableSimulator.Application.OAuth;

/// <summary>Tokeny OAuth pre MCP: tvorba, rozlisenie od pristupovych klucov a overenie PKCE.</summary>
public static class OAuthSecrets
{
    /// <summary>Zaciatok pristupoveho tokenu - odlisi ho od pristupoveho kluca (<see cref="AccessKeys.SecretPrefix" />).</summary>
    public const string AccessPrefix = "tso_";

    public const string RefreshPrefix = "tsr_";

    /// <summary>Pristupovy token plati hodinu - po zruseni povolenia alebo zmene pouzivatela prestane platit najneskor vtedy.</summary>
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromHours(1);

    /// <summary>Obnovovaci token plati od posledneho pouzitia.</summary>
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    public static bool IsAccessToken(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return secret.StartsWith(AccessPrefix, StringComparison.Ordinal);
    }

    /// <summary>Nahodna hodnota: 32 bajtov v base64url za <paramref name="prefix" />.</summary>
    public static string New(string prefix) => prefix + Base64Url(RandomNumberGenerator.GetBytes(32));

    /// <summary>Vyzva PKCE S256 k overovaciemu kodu: base64url(SHA-256(verifier)).</summary>
    public static string Challenge(string verifier)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        return Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
