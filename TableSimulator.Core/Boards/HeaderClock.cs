namespace TableSimulator.Core.Boards;

/// <summary>
/// Hodiny v hlavicke - ukazuju cas hodin panelu, ktory nastavuje INISS (kym ho nenastavi, cas pocitaca).
/// </summary>
/// <param name="X">lavy okraj v bodoch</param>
/// <param name="Y">horny okraj v bodoch od vrchu hlavicky</param>
/// <param name="Height">vyska cislic v bodoch</param>
public sealed record HeaderClock(int X, int Y, int Height);
