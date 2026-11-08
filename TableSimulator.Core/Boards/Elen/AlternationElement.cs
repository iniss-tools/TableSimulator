namespace TableSimulator.Core.Boards.Elen;

/// <summary>
/// Striedanie - tabula na tom istom mieste zobrazuje casti postupne.
/// </summary>
/// <param name="Parts">casti, kazda je postupnost usekov</param>
public sealed record AlternationElement(IReadOnlyList<IReadOnlyList<TextRun>> Parts) : RowElement;
