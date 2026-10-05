namespace TableSimulator.Application.Audit;

/// <summary>Audit v databaze - implementuje databaza.</summary>
public interface IAuditLog
{
    /// <summary>Zapise zaznam.</summary>
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken);

    /// <summary>Zaznamy od najnovsieho; o jeden viac nez <paramref name="limit" />, ak su aj starsie.</summary>
    /// <param name="before">len zaznamy s mensim Id; null = od najnovsieho</param>
    /// <param name="user">len zaznamy pouzivatela; null = vsetkych</param>
    /// <param name="action">len zaznamy akcie; null = vsetkych</param>
    /// <param name="limit">pocet zaznamov</param>
    /// <param name="cancellationToken">zrusenie</param>
    Task<IReadOnlyList<AuditEntry>> ListAsync(long? before, string? user, string? action, int limit, CancellationToken cancellationToken);

    /// <summary>Odstrani zaznamy starsie nez <paramref name="time" /> (UTC); vrati pocet.</summary>
    Task<int> DeleteOlderThanAsync(DateTime time, CancellationToken cancellationToken);
}
