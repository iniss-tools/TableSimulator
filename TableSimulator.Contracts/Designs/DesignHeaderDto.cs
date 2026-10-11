namespace TableSimulator.Contracts.Designs;

/// <summary>
/// Pismo a farby cela tabule (kluc <c>header</c> z casov, ked celom bola len hlavicka nad bodmi).
/// </summary>
/// <param name="Background">pozadie pasov (a panelov, ak nemaju vlastne)</param>
/// <param name="Color">farba textov</param>
/// <param name="Font">pismo textov (CSS font-family)</param>
/// <param name="Bold">tucne texty</param>
/// <param name="ClockColor">farba hodin</param>
/// <param name="ClockBackground">pozadie hodin</param>
/// <param name="ClockFont">pismo hodin (CSS font-family)</param>
/// <param name="Panel">pozadie panelov (cislo kolaje); null = ako pasy</param>
/// <param name="Styles">pomenovane styly textov cela (<c>title</c>, <c>label</c>, <c>number</c>); null = ziadne</param>
public sealed record DesignHeaderDto(
    string Background,
    string Color,
    string Font,
    bool Bold,
    string ClockColor,
    string ClockBackground,
    string ClockFont,
    string? Panel = null,
    IReadOnlyDictionary<string, DesignTextStyleDto>? Styles = null);
