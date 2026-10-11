namespace TableSimulator.Application.Faces;

/// <summary>Vlastne predlohy ciel tabul - implementuje databaza.</summary>
public interface IFaceRepository
{
    /// <summary>Vsetky vlastne predlohy podla kluca.</summary>
    Task<IReadOnlyList<FaceRecord>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Predloha s klucom; null, ak neexistuje.</summary>
    Task<FaceRecord?> FindAsync(string id, CancellationToken cancellationToken);

    /// <summary>Zalozi alebo prepise predlohu.</summary>
    Task SaveAsync(FaceRecord face, CancellationToken cancellationToken);

    /// <summary>Odstrani predlohu; false, ak neexistuje.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
