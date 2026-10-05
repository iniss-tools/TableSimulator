namespace TableSimulator.Application.Virtual;

/// <summary>Vlastne rozlozenia virtualnych tabul - implementuje databaza.</summary>
public interface IVirtualLayoutRepository
{
    /// <summary>Vsetky vlastne rozlozenia podla kluca.</summary>
    Task<IReadOnlyList<VirtualLayoutRecord>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Rozlozenie s klucom; null, ak neexistuje.</summary>
    Task<VirtualLayoutRecord?> FindAsync(string id, CancellationToken cancellationToken);

    /// <summary>Zalozi alebo prepise rozlozenie.</summary>
    Task SaveAsync(VirtualLayoutRecord layout, CancellationToken cancellationToken);

    /// <summary>Odstrani rozlozenie; false, ak neexistuje.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
