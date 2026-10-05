namespace TableSimulator.Contracts.Boards;

/// <summary>Hlavicka tabule - stitok nad maticou s textami a hodinami.</summary>
public sealed record BoardHeaderDto
{
    /// <summary>Vyska hlavicky v bodoch.</summary>
    public int Height { get; init; }

    /// <summary>Texty hlavicky.</summary>
    public IReadOnlyList<HeaderTextDto> Texts { get; init; } = [];

    /// <summary>Hodiny panelu; null = bez hodin.</summary>
    public HeaderClockDto? Clock { get; init; }
}
