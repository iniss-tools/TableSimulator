using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.OAuth;

namespace TableSimulator.Persistence.OAuth;

/// <summary>Klienti a povolenia OAuth pre MCP v databaze.</summary>
public sealed class OAuthRepository(AppDbContext db) : IOAuthRepository
{
    public async Task AddClientAsync(OAuthClient client, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        db.OAuthClients.Add(client);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public Task<OAuthClient?> FindClientAsync(string id, CancellationToken cancellationToken) =>
        db.OAuthClients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<int> DeleteUnusedClientsAsync(DateTime before, CancellationToken cancellationToken) =>
        db.OAuthClients.Where(c => c.CreatedAt < before && !db.OAuthGrants.Any(g => g.ClientId == c.Id)).ExecuteDeleteAsync(cancellationToken);

    public async Task AddGrantAsync(OAuthGrant grant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grant);
        db.OAuthGrants.Add(grant);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public async Task<IReadOnlyList<OAuthGrant>> ListGrantsAsync(CancellationToken cancellationToken) =>
        await db.OAuthGrants.AsNoTracking().OrderBy(g => g.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<OAuthGrant?> FindByAccessHashAsync(string hash, CancellationToken cancellationToken) =>
        db.OAuthGrants.AsNoTracking().FirstOrDefaultAsync(g => g.AccessHash == hash, cancellationToken);

    public Task<OAuthGrant?> FindByRefreshHashAsync(string hash, CancellationToken cancellationToken) =>
        db.OAuthGrants.AsNoTracking().FirstOrDefaultAsync(g => g.RefreshHash == hash || g.PreviousRefreshHash == hash, cancellationToken);

    public async Task<bool> RotateAsync(Guid id, string refreshHash, OAuthGrant updated, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updated);
        return await db.OAuthGrants.Where(g => g.Id == id && g.RefreshHash == refreshHash).ExecuteUpdateAsync(s => s
            .SetProperty(g => g.AccessHash, updated.AccessHash)
            .SetProperty(g => g.AccessExpiresAt, updated.AccessExpiresAt)
            .SetProperty(g => g.RefreshHash, updated.RefreshHash)
            .SetProperty(g => g.RefreshExpiresAt, updated.RefreshExpiresAt)
            .SetProperty(g => g.PreviousRefreshHash, updated.PreviousRefreshHash)
            .SetProperty(g => g.LastUsedAt, updated.LastUsedAt), cancellationToken).ConfigureAwait(false) > 0;
    }

    public Task TouchAsync(Guid id, DateTime time, CancellationToken cancellationToken) =>
        db.OAuthGrants.Where(g => g.Id == id).ExecuteUpdateAsync(s => s.SetProperty(g => g.LastUsedAt, time), cancellationToken);

    public async Task<bool> DeleteGrantAsync(Guid id, CancellationToken cancellationToken) =>
        await db.OAuthGrants.Where(g => g.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;

    public Task<int> DeleteExpiredGrantsAsync(DateTime now, CancellationToken cancellationToken) =>
        db.OAuthGrants.Where(g => g.RefreshExpiresAt < now).ExecuteDeleteAsync(cancellationToken);
}
