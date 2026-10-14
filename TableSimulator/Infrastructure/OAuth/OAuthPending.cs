using TableSimulator.Application.OAuth;

namespace TableSimulator.Infrastructure.OAuth;

/// <summary>Ziadost o povolenie cakajuca na rozhodnutie pouzivatela na stranke povolenia.</summary>
public sealed record OAuthRequest(string Id, OAuthClient Client, string RedirectUri, string CodeChallenge, string? State, string UserName, DateTime ExpiresAt);

/// <summary>Jednorazovy kod po povoleni - klient ho vymeni za tokeny.</summary>
public sealed record OAuthCode(string ClientId, string RedirectUri, string CodeChallenge, string UserName, string Role, DateTime ExpiresAt);

/// <summary>
/// Kratkodobe kroky povolenia len v pamati: ziadosti cakajuce na pouzivatela (<see cref="RequestLifetime" />) a jednorazove
/// kody (<see cref="CodeLifetime" />). Po restarte servera sa povolenie zopakuje.
/// </summary>
public sealed class OAuthPending(TimeProvider time)
{
    public static readonly TimeSpan RequestLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromSeconds(60);

    private readonly Lock _lock = new();
    private readonly Dictionary<string, OAuthRequest> _requests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OAuthCode> _codes = new(StringComparer.Ordinal);

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public OAuthRequest CreateRequest(OAuthClient client, string redirectUri, string codeChallenge, string? state, string userName)
    {
        var request = new OAuthRequest(OAuthSecrets.New(""), client, redirectUri, codeChallenge, state, userName, Now + RequestLifetime);
        lock (_lock)
        {
            Expire(_requests, r => r.ExpiresAt);
            _requests[request.Id] = request;
        }
        return request;
    }

    /// <summary>Ziadost na rozhodnutie - jednorazovo (rozhodnutie ju odstrani); null = neexistuje alebo vyprsala.</summary>
    public OAuthRequest? TakeRequest(string? id)
    {
        lock (_lock)
            return id is not null && _requests.Remove(id, out var request) && request.ExpiresAt >= Now ? request : null;
    }

    /// <summary>Povolenie pouzivatela - jednorazovy kod pre klienta.</summary>
    public string Approve(OAuthRequest request, string role)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = OAuthSecrets.New("");
        lock (_lock)
        {
            Expire(_codes, c => c.ExpiresAt);
            _codes[code] = new OAuthCode(request.Client.Id, request.RedirectUri, request.CodeChallenge, request.UserName, role, Now + CodeLifetime);
        }
        return code;
    }

    /// <summary>Kod na vymenu - jednorazovo; null = neexistuje alebo vyprsal.</summary>
    public OAuthCode? TakeCode(string? code)
    {
        lock (_lock)
            return code is not null && _codes.Remove(code, out var stored) && stored.ExpiresAt >= Now ? stored : null;
    }

    private void Expire<T>(Dictionary<string, T> items, Func<T, DateTime> expiresAt)
    {
        foreach (var key in items.Where(i => expiresAt(i.Value) < Now).Select(i => i.Key).ToList())
            items.Remove(key);
    }
}
