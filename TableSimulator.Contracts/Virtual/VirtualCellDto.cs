namespace TableSimulator.Contracts.Virtual;

/// <summary>
/// Polozka zaznamu - meno prvku z XML INISSu (<c>time</c>, <c>type</c>, <c>number</c>, <c>destination</c>…) a text;
/// casti oddelene <c>#</c> sa striedaju, text konciaci <c>#</c> blika.
/// </summary>
public sealed record VirtualCellDto(string Name, string Text);
