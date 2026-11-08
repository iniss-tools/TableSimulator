using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;

namespace TableSimulator.Core.Scenarios;

/// <summary>Co scenar v danej chvili nastavuje.</summary>
/// <param name="Faults">porucha tabule podla identifikatora</param>
/// <param name="PoweredOff">tabule bez napajania</param>
/// <param name="Outages">linky s vypadkom</param>
public sealed record ScenarioEffects(IReadOnlyDictionary<string, BoardFault> Faults, IReadOnlySet<string> PoweredOff, IReadOnlySet<int> Outages)
{
    /// <summary>Prekrytie ustredne AK Signal podla identifikatora (tabule ineho druhu ho nepouziju).</summary>
    public IReadOnlyDictionary<string, AkSignalOverride> Devices { get; init; } = new Dictionary<string, AkSignalOverride>();
}
