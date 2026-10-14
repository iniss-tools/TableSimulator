namespace TableSimulator.Application.OAuth;

/// <summary>Ziadost klienta o povolenie cakajuca na rozhodnutie pouzivatela na stranke povolenia (<c>#/authorize/{Id}</c>).</summary>
/// <param name="Id">nahodny identifikator v adrese stranky</param>
/// <param name="Client">klient</param>
/// <param name="RedirectUri">adresa navratu z ziadosti (jedna z registrovanych)</param>
/// <param name="CodeChallenge">vyzva PKCE S256</param>
/// <param name="State">hodnota klienta, ktoru dostane spat</param>
/// <param name="Issuer">adresa servera - ide spat ako <c>iss</c> (RFC 9207)</param>
/// <param name="ExpiresAt">koniec platnosti (UTC)</param>
public sealed record OAuthRequest(string Id, OAuthClient Client, string RedirectUri, string CodeChallenge, string? State, string Issuer, DateTime ExpiresAt);

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

    public OAuthRequest CreateRequest(OAuthClient client, string redirectUri, string codeChallenge, string? state, string issuer)
    {
        var request = new OAuthRequest(OAuthSecrets.New(""), client, redirectUri, codeChallenge, state, issuer, Now + RequestLifetime);
        lock (_lock)
        {
            Expire(_requests, r => r.ExpiresAt);
            _requests[request.Id] = request;
        }
        return request;
    }

    /// <summary>Ziadost na zobrazenie; null = neexistuje alebo vyprsala.</summary>
    public OAuthRequest? FindRequest(string id)
    {
        lock (_lock)
            return _requests.TryGetValue(id, out var request) && request.ExpiresAt >= Now ? request : null;
    }

    /// <summary>Ziadost na rozhodnutie - jednorazovo (rozhodnutie ju odstrani); null = neexistuje alebo vyprsala.</summary>
    public OAuthRequest? TakeRequest(string id)
    {
        lock (_lock)
            return _requests.Remove(id, out var request) && request.ExpiresAt >= Now ? request : null;
    }

    /// <summary>Povolenie pouzivatela <paramref name="userName" /> s rolou <paramref name="role" /> - jednorazovy kod pre klienta.</summary>
    public string Approve(OAuthRequest request, string userName, string role)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = OAuthSecrets.New("");
        lock (_lock)
        {
            Expire(_codes, c => c.ExpiresAt);
            _codes[code] = new OAuthCode(request.Client.Id, request.RedirectUri, request.CodeChallenge, userName, role, Now + CodeLifetime);
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
