namespace TableSimulator.Contracts.Designs;

/// <summary>Hlavicka tabule (stitok s textami a hodiny).</summary>
/// <param name="Background">pozadie</param>
/// <param name="Color">farba textov</param>
/// <param name="Font">pismo textov (CSS font-family)</param>
/// <param name="Bold">tucne texty</param>
/// <param name="ClockColor">farba hodin</param>
/// <param name="ClockBackground">pozadie hodin</param>
/// <param name="ClockFont">pismo hodin (CSS font-family)</param>
public sealed record DesignHeaderDto(string Background, string Color, string Font, bool Bold, string ClockColor, string ClockBackground, string ClockFont);
