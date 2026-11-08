namespace TableSimulator.Core.Monitor;

/// <summary>
/// Text hlavicky tabule. Polohy a vyska su v bodoch tabule.
/// </summary>
/// <param name="X">vodorovna poloha laveho okraja</param>
/// <param name="Y">zvisla poloha horneho okraja od vrchu hlavicky</param>
/// <param name="Height">vyska pisma</param>
/// <param name="Text">text</param>
public sealed record MonitorHeaderText(int X, int Y, int Height, string Text);
