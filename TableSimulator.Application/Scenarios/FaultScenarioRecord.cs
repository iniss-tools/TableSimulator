using System.Text.Json;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Scenarios;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Scenarios;

namespace TableSimulator.Application.Scenarios;

/// <summary>Scenar porucha v databaze; obsah (<see cref="FaultScenarioRequest" />) ako JSON.</summary>
public sealed record FaultScenarioRecord(int Id, string Name, string Definition);

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

/// <summary>Prevod scenara medzi API, databazou a jadrom.</summary>
public static class ScenarioMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Serialize(FaultScenarioRequest request) => JsonSerializer.Serialize(request, Json);

    public static FaultScenarioRequest Deserialize(string json) =>
        JsonSerializer.Deserialize<FaultScenarioRequest>(json, Json) ?? throw new JsonException("Empty scenario definition.");

    /// <summary>Scenar pre jadro; nazov sa orezava.</summary>
    public static FaultScenario ToCore(FaultScenarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new FaultScenario(request.Name.Trim(), request.Loop, request.CycleMs, request.Steps.Select(ToCore).ToList());
    }

    private static ScenarioStep ToCore(ScenarioStepDto step) => new(step.AtMs, step.DurationMs, step.Line, step.Address, step.Action switch
    {
        Contracts.Scenarios.ScenarioAction.Fault => Core.Scenarios.ScenarioAction.Fault,
        Contracts.Scenarios.ScenarioAction.PowerLoss => Core.Scenarios.ScenarioAction.PowerLoss,
        Contracts.Scenarios.ScenarioAction.LineOutage => Core.Scenarios.ScenarioAction.LineOutage,
        _ => throw new ArgumentOutOfRangeException(nameof(step), step.Action, null)
    })
    {
        Fault = step.Fault is { } fault ? RequestMapper.ToFault(fault) : BoardFault.None
    };

    /// <summary>Scenar pre API s behom (ak bezi).</summary>
    public static FaultScenarioDto ToDto(FaultScenarioRecord record, ScenarioRunDto? run)
    {
        ArgumentNullException.ThrowIfNull(record);
        var request = Deserialize(record.Definition);
        return new FaultScenarioDto(record.Id, record.Name, request.Loop, request.CycleMs, request.Steps, run);
    }
}
