using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Application.Scenarios;

/// <summary>Scenare porucha s behom.</summary>
public sealed record GetFaultScenariosQuery : IQuery<IReadOnlyList<FaultScenarioDto>>;

/// <summary>Zalozi scenar porucha.</summary>
public sealed record CreateFaultScenarioCommand(FaultScenarioRequest Request) : ICommand<FaultScenarioDto>, IAuditable
{
    string IAuditable.AuditTarget => Request.Name;
}

/// <summary>Zmeni scenar porucha; beziaci pokracuje s novymi krokmi.</summary>
public sealed record SetFaultScenarioCommand(int Id, FaultScenarioRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Odstrani scenar porucha (beziaci zastavi).</summary>
public sealed record DeleteFaultScenarioCommand(int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Spusti scenar porucha od zaciatku.</summary>
public sealed record StartFaultScenarioCommand(int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Zastavi scenar porucha a vrati, co nastavil.</summary>
public sealed record StopFaultScenarioCommand(int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}
