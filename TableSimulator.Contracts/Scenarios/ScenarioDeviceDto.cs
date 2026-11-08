using TableSimulator.Contracts.Boards;

namespace TableSimulator.Contracts.Scenarios;

/// <summary>
/// Co krok scenara nastavuje ustredni AK Signal: stavy sa pridaju k stavom z jej nastavenia a snimac sa prepne do
/// daneho rezimu. Plati len pocas kroku, nastavenie ustredne sa nemeni.
/// </summary>
/// <param name="Conditions">stavy pridane k stavom ustredne</param>
/// <param name="Sensor">rezim snimaca tmy; null = podla nastavenia ustredne</param>
public sealed record ScenarioDeviceDto(AkSignalConditionsDto Conditions, TwilightMode? Sensor);
