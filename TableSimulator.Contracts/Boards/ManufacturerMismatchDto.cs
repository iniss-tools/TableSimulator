namespace TableSimulator.Contracts.Boards;

/// <summary>INISS posiela tabuli spravy pre ineho vyrobcu.</summary>
/// <param name="Evidence">co na to ukazuje (<c>elen.extendedFont</c>, <c>lcd.lcd1Commands</c>, <c>lcd.lcdCommands</c>)</param>
/// <param name="Suggested">vyrobcovia, ktorym take spravy patria</param>
public sealed record ManufacturerMismatchDto(string Evidence, IReadOnlyList<string> Suggested);
