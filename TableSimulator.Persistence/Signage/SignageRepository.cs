using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Signage;

namespace TableSimulator.Persistence.Signage;

/// <summary>Spravy, playlisty, skupiny, panely a nahrate subory informacnych panelov v databaze.</summary>
public sealed class SignageRepository(AppDbContext db) : ISignageRepository
{
    public async Task<SignageRecords> LoadAsync(CancellationToken cancellationToken) =>
        new(
            await db.SignageMessages.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
            await db.SignagePlaylists.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
            await db.SignageGroups.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
            await db.SignagePanels.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
            await db.SignageMedia.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false));

    public Task<int> SaveMessageAsync(SignageMessageRecord message, CancellationToken cancellationToken) =>
        SaveAsync(db.SignageMessages, message, message.Id, m => m.Id, cancellationToken);

    public Task<int> SavePlaylistAsync(SignagePlaylistRecord playlist, CancellationToken cancellationToken) =>
        SaveAsync(db.SignagePlaylists, playlist, playlist.Id, p => p.Id, cancellationToken);

    public Task<int> SaveGroupAsync(SignageGroupRecord group, CancellationToken cancellationToken) =>
        SaveAsync(db.SignageGroups, group, group.Id, g => g.Id, cancellationToken);

    public Task<int> SavePanelAsync(SignagePanelRecord panel, CancellationToken cancellationToken) =>
        SaveAsync(db.SignagePanels, panel, panel.Id, p => p.Id, cancellationToken);

    public async Task<bool> DeleteAsync(SignageEntity entity, int id, CancellationToken cancellationToken)
    {
        var deleted = entity switch
        {
            SignageEntity.Message => await db.SignageMessages.Where(m => m.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
            SignageEntity.Playlist => await db.SignagePlaylists.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
            SignageEntity.Group => await db.SignageGroups.Where(g => g.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
            _ => await db.SignagePanels.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false)
        };
        return deleted > 0;
    }

    public async Task AddMediaAsync(SignageMediaRecord media, CancellationToken cancellationToken)
    {
        db.SignageMedia.Add(media);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public Task<SignageMediaRecord?> FindMediaAsync(Guid id, CancellationToken cancellationToken) =>
        db.SignageMedia.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task DeleteMediaAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.SignageMedia.Where(m => ids.Contains(m.Id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>Cislo 0 = novy zaznam (cislo prideli databaza); inak prepise existujuci, 0 = neexistuje.</summary>
    private async Task<int> SaveAsync<T>(DbSet<T> set, T record, int id, Func<T, int> idOf, CancellationToken cancellationToken) where T : class
    {
        if (id == 0)
        {
            set.Add(record);
        }
        else
        {
            var exists = await set.FindAsync([id], cancellationToken).ConfigureAwait(false);
            if (exists is null)
                return 0;
            db.Entry(exists).State = EntityState.Detached;
            set.Update(record);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var saved = idOf(record);
        db.ChangeTracker.Clear();
        return saved;
    }
}
