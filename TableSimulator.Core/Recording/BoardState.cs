namespace TableSimulator.Core.Recording;

/// <summary>Co tabula ukazovala od casu <paramref name="Time" /> (s od zaciatku zaznamu) - riadky textu zhora.</summary>
public sealed record BoardState(double Time, IReadOnlyList<string> Rows)
{
    /// <summary>Kedy sa tabula ustalila - posledna zmena, ktora k stavu patri [s]; na zobrazenie tabule v tomto stave.</summary>
    public double Settled { get; init; } = Time;

    /// <summary>Kluc na porovnanie obsahu (bez casu).</summary>
    public string Key { get; } = string.Join('\n', Rows);
}
