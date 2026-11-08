namespace TableSimulator.Core.Boards;

/// <summary>
/// Text hlavicky.
/// </summary>
/// <param name="X">lavy okraj v bodoch</param>
/// <param name="Y">horny okraj v bodoch od vrchu hlavicky</param>
/// <param name="Height">vyska pisma v bodoch</param>
/// <param name="Text">text</param>
public sealed record HeaderText(int X, int Y, int Height, string Text);
