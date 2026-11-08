namespace TableSimulator.Core.Boards;

/// <summary>
/// Hlavicka tabule: pevny stitok nad maticou (napr. "ODCHODY / DEPARTURES" a popisy stlpcov) a hodiny panelu.
/// INISS ju neposiela - patri k hardveru tabule. Polohy a vysky su v bodoch matice.
/// </summary>
public sealed record BoardHeader
{
    /// <summary>Vyska hlavicky v bodoch.</summary>
    public int Height { get; init; }

    /// <summary>Texty hlavicky.</summary>
    public IReadOnlyList<HeaderText> Texts { get; init; } = [];

    /// <summary>Hodiny panelu; null = panel hodiny nema.</summary>
    public HeaderClock? Clock { get; init; }
}
