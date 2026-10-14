using Microsoft.Extensions.Logging;
using TableSimulator.Application.Keys;
using TableSimulator.Application.OAuth;

namespace TableSimulator.Infrastructure.OAuth;

/// <summary>Tokeny vydane klientovi (odpoved /oauth/token).</summary>
public sealed record OAuthTokens(string AccessToken, string RefreshToken, TimeSpan ExpiresIn);

/// <summary>
/// Server OAuth pre MCP: registracia klientov, vymena kodu za tokeny (PKCE S256), obnovovanie s vymenou obnovovacieho
/// tokenu a overenie pristupoveho tokenu. Pouzitie uz vymeneneho obnovovacieho tokenu (unik) povolenie zrusi;
/// pri obnoveni a najviac raz za minutu pri pristupe sa overi pouzivatel povolenia.
/// </summary>
public sealed class OAuthServer(IOAuthRepository repository, OAuthPending pending, OAuthUsers users, TimeProvider time, ILogger<OAuthServer> logger)
{
    /// <summary>Klient bez povolenia sa po tomto case zmaze - VS Code sa registruje pri kazdom pripojeni.</summary>
    public static readonly TimeSpan UnusedClientLifetime = TimeSpan.FromDays(1);

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task<OAuthClient> RegisterAsync(string name, string[] redirectUris, CancellationToken cancellationToken)
    {
        await repository.DeleteUnusedClientsAsync(Now - UnusedClientLifetime, cancellationToken).ConfigureAwait(false);
        await repository.DeleteExpiredGrantsAsync(Now, cancellationToken).ConfigureAwait(false);
        var client = new OAuthClient(Guid.NewGuid().ToString("N"), name, redirectUris, Now);
        await repository.AddClientAsync(client, cancellationToken).ConfigureAwait(false);
        OAuthLog.Registered(logger, client.Name, redirectUris);
        return client;
    }

    public Task<OAuthClient?> FindClientAsync(string? id, CancellationToken cancellationToken) =>
        id is null ? Task.FromResult<OAuthClient?>(null) : repository.FindClientAsync(id, cancellationToken);

    /// <summary>Vymena kodu za tokeny; null + kod chyby OAuth pri neuspechu.</summary>
    public async Task<(OAuthTokens? Tokens, string? Error)> ExchangeCodeAsync(string? code, string? clientId, string? redirectUri, string? verifier,
        CancellationToken cancellationToken)
    {
        var stored = pending.TakeCode(code);
        if (stored is null || stored.ClientId != clientId || stored.RedirectUri != redirectUri || verifier is null
            || OAuthSecrets.Challenge(verifier) != stored.CodeChallenge)
            return (null, "invalid_grant");
        if (await repository.FindClientAsync(stored.ClientId, cancellationToken).ConfigureAwait(false) is not { } client)
            return (null, "invalid_client");

        var (tokens, access, refresh) = Issue();
        await repository.AddGrantAsync(new OAuthGrant(Guid.NewGuid(), client.Id, client.Name, stored.UserName, stored.Role, Now, Now,
            access, Now + OAuthSecrets.AccessLifetime, refresh, Now + OAuthSecrets.RefreshLifetime, null), cancellationToken).ConfigureAwait(false);
        return (tokens, null);
    }

    /// <summary>Obnovenie: novy pristupovy aj obnovovaci token; null + kod chyby OAuth pri neuspechu.</summary>
    public async Task<(OAuthTokens? Tokens, string? Error)> RefreshAsync(string? refreshToken, string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return (null, "invalid_request");
        var hash = AccessKeys.Hash(refreshToken);
        var grant = await repository.FindByRefreshHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (grant is null)
            return (null, "invalid_grant");
        if (grant.RefreshHash != hash)
        {
            // uz vymeneny token - pouzil ho niekto iny nez klient (alebo naopak); povolenie konci
            await repository.DeleteGrantAsync(grant.Id, cancellationToken).ConfigureAwait(false);
            OAuthLog.RefreshReused(logger, grant.ClientName, grant.UserName);
            return (null, "invalid_grant");
        }
        if (grant.ClientId != clientId || grant.RefreshExpiresAt < Now)
            return (null, "invalid_grant");
        if (!await users.AllowsAsync(grant).ConfigureAwait(false))
        {
            await Revoke(grant, cancellationToken).ConfigureAwait(false);
            return (null, "invalid_grant");
        }

        var (tokens, access, refresh) = Issue();
        var updated = grant with
        {
            AccessHash = access,
            AccessExpiresAt = Now + OAuthSecrets.AccessLifetime,
            RefreshHash = refresh,
            RefreshExpiresAt = Now + OAuthSecrets.RefreshLifetime,
            PreviousRefreshHash = hash,
            LastUsedAt = Now
        };
        // sucasne obnovenie toho isteho tokenu - uspeje len jedno
        return await repository.RotateAsync(grant.Id, hash, updated, cancellationToken).ConfigureAwait(false) ? (tokens, null) : (null, "invalid_grant");
    }

    /// <summary>Povolenie platneho pristupoveho tokenu; null = neplatny, prepadnuty alebo pouzivatel uz pristup dat nemoze.</summary>
    public async Task<OAuthGrant?> AuthenticateAsync(string token, CancellationToken cancellationToken)
    {
        var grant = await repository.FindByAccessHashAsync(AccessKeys.Hash(token), cancellationToken).ConfigureAwait(false);
        if (grant is null || grant.AccessExpiresAt < Now)
            return null;
        if (grant.LastUsedAt is { } used && Now - used <= AccessKeys.TouchInterval)
            return grant;
        if (!await users.AllowsAsync(grant).ConfigureAwait(false))
        {
            await Revoke(grant, cancellationToken).ConfigureAwait(false);
            return null;
        }
        await repository.TouchAsync(grant.Id, Now, cancellationToken).ConfigureAwait(false);
        return grant;
    }

    private async Task Revoke(OAuthGrant grant, CancellationToken cancellationToken)
    {
        await repository.DeleteGrantAsync(grant.Id, cancellationToken).ConfigureAwait(false);
        OAuthLog.UserNotAllowed(logger, grant.ClientName, grant.UserName);
    }

    /// <summary>Nove tokeny a ich hashe do databazy.</summary>
    private static (OAuthTokens Tokens, string AccessHash, string RefreshHash) Issue()
    {
        var access = OAuthSecrets.New(OAuthSecrets.AccessPrefix);
        var refresh = OAuthSecrets.New(OAuthSecrets.RefreshPrefix);
        return (new OAuthTokens(access, refresh, OAuthSecrets.AccessLifetime), AccessKeys.Hash(access), AccessKeys.Hash(refresh));
    }
}
