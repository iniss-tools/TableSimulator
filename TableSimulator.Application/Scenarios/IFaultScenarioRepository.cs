namespace TableSimulator.Application.Scenarios;

/// <summary>Scenare porucha v databaze.</summary>
public interface IFaultScenarioRepository
{
    Task<IReadOnlyList<FaultScenarioRecord>> ListAsync(CancellationToken cancellationToken);

    Task<FaultScenarioRecord?> FindAsync(int id, CancellationToken cancellationToken);

    /// <summary>Prida scenar; vrati jeho cislo.</summary>
    Task<int> AddAsync(string name, string definition, CancellationToken cancellationToken);

    /// <summary>Zmeni scenar; false = taky nie je.</summary>
    Task<bool> UpdateAsync(FaultScenarioRecord scenario, CancellationToken cancellationToken);

    /// <summary>Odstrani scenar; false = taky nie je.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
