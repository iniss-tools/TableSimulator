namespace TableSimulator.Application.Virtual;

/// <summary>Ulozene definicie virtualnych tabul - implementuje databaza.</summary>
public interface IVirtualBoardRepository
{
    /// <summary>Vsetky tabule podla cisla.</summary>
    Task<IReadOnlyList<VirtualBoard>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Tabula s cislom; null, ak neexistuje.</summary>
    Task<VirtualBoard?> FindAsync(int id, CancellationToken cancellationToken);

    /// <summary>Zalozi alebo prepise tabulu s cislom <see cref="VirtualBoard.Id" />; true, ak je nova.</summary>
    Task<bool> SaveAsync(VirtualBoard board, CancellationToken cancellationToken);

    /// <summary>Odstrani tabulu; false, ak neexistuje.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Ulozi viac tabul naraz (v jednej transakcii). Tabulu s cislom, ktore uz existuje, prepise len pri
    /// <paramref name="replace" />.
    /// </summary>
    Task<VirtualImportCounts> ImportAsync(IReadOnlyList<VirtualBoard> boards, bool replace, CancellationToken cancellationToken);
}

/// <summary>Kolko tabul import pridal, prepisal a vynechal.</summary>
public readonly record struct VirtualImportCounts(int Added, int Replaced, int Skipped);
