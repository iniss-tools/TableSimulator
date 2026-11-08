namespace TableSimulator.Core.Boards;

/// <summary>
/// Obsah tabule - podla rodiny protokolu.
/// </summary>
public abstract class BoardDisplay
{
    /// <summary>Jas 0-100; 0 = zhasnuta.</summary>
    public int Brightness { get; set; } = 100;

    /// <summary>Cas hodin tabule, ako ho naposledy nastavil INISS; null = nenastaveny.</summary>
    public DateTime? ClockValue { get; set; }

    /// <summary>Kedy INISS hodiny nastavil.</summary>
    public DateTimeOffset? ClockSetAt { get; set; }

    /// <summary>Zmaze obsah (po zapnuti alebo prikazom zmazania).</summary>
    public abstract void Clear();
}
