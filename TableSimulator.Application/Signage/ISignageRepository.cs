namespace TableSimulator.Application.Signage;

/// <summary>Spravy, playlisty, skupiny, panely a nahrate subory - implementuje databaza.</summary>
public interface ISignageRepository
{
    /// <summary>Vsetko naraz.</summary>
    Task<SignageRecords> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Ulozi spravu (cislo 0 = nova); vrati jej cislo, 0 = sprava s cislom neexistuje.</summary>
    Task<int> SaveMessageAsync(SignageMessageRecord message, CancellationToken cancellationToken);

    /// <summary>Ulozi playlist (cislo 0 = novy); vrati jeho cislo, 0 = neexistuje.</summary>
    Task<int> SavePlaylistAsync(SignagePlaylistRecord playlist, CancellationToken cancellationToken);

    /// <summary>Ulozi skupinu (cislo 0 = nova); vrati jej cislo, 0 = neexistuje.</summary>
    Task<int> SaveGroupAsync(SignageGroupRecord group, CancellationToken cancellationToken);

    /// <summary>Ulozi panel (cislo 0 = novy); vrati jeho cislo, 0 = neexistuje.</summary>
    Task<int> SavePanelAsync(SignagePanelRecord panel, CancellationToken cancellationToken);

    /// <summary>Odstrani spravu, playlist, skupinu alebo panel; false, ak neexistuje.</summary>
    Task<bool> DeleteAsync(SignageEntity entity, int id, CancellationToken cancellationToken);

    /// <summary>Zapise nahraty subor.</summary>
    Task AddMediaAsync(SignageMediaRecord media, CancellationToken cancellationToken);

    /// <summary>Nahraty subor; null, ak neexistuje.</summary>
    Task<SignageMediaRecord?> FindMediaAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Odstrani zaznamy nahratych suborov (subory maze <see cref="SignageMediaStore" />).</summary>
    Task DeleteMediaAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
