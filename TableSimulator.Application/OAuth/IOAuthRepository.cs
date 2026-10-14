namespace TableSimulator.Application.OAuth;

/// <summary>Klienti a povolenia OAuth pre MCP - implementuje databaza.</summary>
public interface IOAuthRepository
{
    Task AddClientAsync(OAuthClient client, CancellationToken cancellationToken);

    Task<OAuthClient?> FindClientAsync(string id, CancellationToken cancellationToken);

    /// <summary>Odstrani klientov bez povolenia zaregistrovanych pred <paramref name="before" /> (klient sa registruje pri kazdom pripojeni).</summary>
    Task<int> DeleteUnusedClientsAsync(DateTime before, CancellationToken cancellationToken);

    Task AddGrantAsync(OAuthGrant grant, CancellationToken cancellationToken);

    /// <summary>Vsetky povolenia podla casu vzniku.</summary>
    Task<IReadOnlyList<OAuthGrant>> ListGrantsAsync(CancellationToken cancellationToken);

    Task<OAuthGrant?> FindByAccessHashAsync(string hash, CancellationToken cancellationToken);

    /// <summary>Povolenie s obnovovacim tokenom - aktualnym alebo naposledy vymenenym.</summary>
    Task<OAuthGrant?> FindByRefreshHashAsync(string hash, CancellationToken cancellationToken);

    /// <summary>
    /// Nove tokeny, len ak ma povolenie stale obnovovaci token <paramref name="refreshHash" /> (dve sucasne obnovenia
    /// toho isteho tokenu - uspeje jedno); false = uz vymeneny.
    /// </summary>
    Task<bool> RotateAsync(Guid id, string refreshHash, OAuthGrant updated, CancellationToken cancellationToken);

    /// <summary>Zapise posledne pouzitie.</summary>
    Task TouchAsync(Guid id, DateTime time, CancellationToken cancellationToken);

    /// <summary>Odstrani povolenie; false, ak neexistuje.</summary>
    Task<bool> DeleteGrantAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Odstrani povolenia s prepadnutym obnovovacim tokenom.</summary>
    Task<int> DeleteExpiredGrantsAsync(DateTime now, CancellationToken cancellationToken);
}
