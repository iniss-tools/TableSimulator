namespace TableSimulator.Contracts.Boards;

/// <summary>Stavy ustredne AK Signal, ktore server cita, ale nemeni - nastavuje ich obsluha simulatora.</summary>
/// <param name="LightsFault">porucha svetiel</param>
/// <param name="HighPriority">bezi hlasenie s vyssou prioritou (napr. miestny mikrofon)</param>
/// <param name="OutputPowerError">chyba vykonu na vystupe rozhlasu</param>
/// <param name="AmpSupplyMissing">chyba napajanie zosilnovaca</param>
/// <param name="AmpCommError">chyba komunikacie so zosilnovacom (len AKSIGNALUH)</param>
/// <param name="BankNotSynced">banka zvukov nie je zosuladena (len AKSIGNALUH)</param>
public sealed record AkSignalConditionsDto(
    bool LightsFault = false,
    bool HighPriority = false,
    bool OutputPowerError = false,
    bool AmpSupplyMissing = false,
    bool AmpCommError = false,
    bool BankNotSynced = false);
