namespace TableSimulator.Core.Boards.AkSignal;

/// <summary>
/// Stavy ustredne, ktore INISS2 cita, ale nemeni - nastavuje ich obsluha simulatora alebo scenar.
/// </summary>
public sealed record AkSignalConditions
{
    /// <summary>Bez poruchy.</summary>
    public static AkSignalConditions None { get; } = new();

    /// <summary>Porucha svetiel - chyba bit <c>0x08</c> / kluc <c>ok</c>.</summary>
    public bool LightsFault { get; init; }

    /// <summary>Bezi hlasenie s vyssou prioritou (napr. miestny mikrofon) - <c>0x02</c> / <c>hiprt</c>; rusi hlasenie servera.</summary>
    public bool HighPriority { get; init; }

    /// <summary>Chyba vykonu na vystupe - <c>0x08</c> zvuku / <c>pwr</c>.</summary>
    public bool OutputPowerError { get; init; }

    /// <summary>Chyba napajanie zosilnovaca - bez <c>0x10</c> zvuku / <c>sup</c>.</summary>
    public bool AmpSupplyMissing { get; init; }

    /// <summary>Chyba komunikacie so zosilnovacom - <c>comerr</c> (len AKSIGNALUH).</summary>
    public bool AmpCommError { get; init; }

    /// <summary>Banka zvukov nie je zosuladena - <c>sync</c> stale (len AKSIGNALUH).</summary>
    public bool BankNotSynced { get; init; }

    /// <summary>Aspon jeden stav.</summary>
    public bool Any => LightsFault || HighPriority || OutputPowerError || AmpSupplyMissing || AmpCommError || BankNotSynced;

    /// <summary>Stavy z tychto aj <paramref name="other" /> (kazdy plati, ak plati v jednom z nich).</summary>
    public AkSignalConditions With(AkSignalConditions other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new AkSignalConditions
        {
            LightsFault = LightsFault || other.LightsFault,
            HighPriority = HighPriority || other.HighPriority,
            OutputPowerError = OutputPowerError || other.OutputPowerError,
            AmpSupplyMissing = AmpSupplyMissing || other.AmpSupplyMissing,
            AmpCommError = AmpCommError || other.AmpCommError,
            BankNotSynced = BankNotSynced || other.BankNotSynced
        };
    }
}
