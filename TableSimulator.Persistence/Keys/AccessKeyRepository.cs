using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Keys;

namespace TableSimulator.Persistence.Keys;

/// <summary>Pristupove kluce v databaze.</summary>
public sealed class AccessKeyRepository(AppDbContext db) : IAccessKeyRepository
{
    public async Task<IReadOnlyList<AccessKey>> ListAsync(CancellationToken cancellationToken) =>
        await db.AccessKeys.AsNoTracking().OrderBy(k => k.Name).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(AccessKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        db.AccessKeys.Add(key);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AccessKeys.Where(k => k.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;

    public Task<AccessKey?> FindByHashAsync(string hash, CancellationToken cancellationToken) =>
        db.AccessKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Hash == hash, cancellationToken);

    public Task TouchAsync(Guid id, DateTime time, CancellationToken cancellationToken) =>
        db.AccessKeys.Where(k => k.Id == id).ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, time), cancellationToken);
}
