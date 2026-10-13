namespace TableSimulator.Contracts.Boards;

/// <summary>Vysledok cakania na zmenu tabule.</summary>
/// <param name="Changed">tabula sa zmenila; false = cas vyprsal bez zmeny</param>
/// <param name="Board">obsah tabule po zmene alebo na konci cakania</param>
public sealed record BoardChangeDto(bool Changed, BoardTextDto Board);
