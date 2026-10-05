namespace TableSimulator.Application.Keys;

/// <summary>Pristupove kluce - implementuje databaza.</summary>
public interface IAccessKeyRepository
{
    /// <summary>Vsetky kluce podla nazvu.</summary>
    Task<IReadOnlyList<AccessKey>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(AccessKey key, CancellationToken cancellationToken);

    /// <summary>Odstrani kluc; false, ak neexistuje.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Kluc s hashom; null = neplatny kluc.</summary>
    Task<AccessKey?> FindByHashAsync(string hash, CancellationToken cancellationToken);

    /// <summary>Zapise posledne pouzitie.</summary>
    Task TouchAsync(Guid id, DateTime time, CancellationToken cancellationToken);
}
