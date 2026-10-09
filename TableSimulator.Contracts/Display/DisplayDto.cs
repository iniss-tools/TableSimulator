namespace TableSimulator.Contracts.Display;

/// <summary>
/// Obsah tabule: rodina <c>elen</c> (riadky <see cref="Rows" />), <c>chars</c> (<see cref="CharRows" />), <c>flaps</c>
/// (<see cref="FlapRows" />) alebo <c>amplifier</c> - spinacia jednotka zosilnovaca ELSVO (<see cref="Amplifier" />).
/// </summary>
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

    /// <summary>Riadky listovej tabule; null pri inej rodine.</summary>
    public FlapRowDto[]? FlapRows { get; init; }

    /// <summary>Listova tabula: do kedy sa listy otacaju; null = stoja.</summary>
    public DateTimeOffset? RotatingUntil { get; init; }

    /// <summary>Zosilnovac ELSVO; null pri inej rodine.</summary>
    public AmplifierDto? Amplifier { get; init; }
}
