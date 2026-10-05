namespace TableSimulator.Contracts.Display;

/// <summary>Obsah tabule: rodina <c>elen</c> (riadky <see cref="Rows" />) alebo <c>chars</c> (<see cref="CharRows" />).</summary>
public sealed record DisplayDto(
    string Family,
    int Brightness,
    DateTime? Clock,
    DateTimeOffset? ClockSetAt,
    RowDto[] Rows,
    int MovingRow,
    int[]? MovingCodes,
    string? MovingText)
{
    /// <summary>Jas zelenej 0-100.</summary>
    public int BrightnessGreen { get; init; } = 100;

    /// <summary>Svietenie blikajuceho textu z prikazu 9 [ms]; null = podla profilu.</summary>
    public int? FlashOnMs { get; init; }

    /// <summary>Zhasnutie blikajuceho textu z prikazu 9 [ms]; null = podla profilu.</summary>
    public int? FlashOffMs { get; init; }

    /// <summary>Riadky znakovej tabule; null pri inej rodine.</summary>
    public CharRowDto[]? CharRows { get; init; }
}
