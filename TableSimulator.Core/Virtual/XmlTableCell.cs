namespace TableSimulator.Core.Virtual;

/// <summary>
/// Polozka zaznamu tabule v XML INISSu - prvok s menom podla druhu obsahu stlpca (<c>time</c>, <c>number</c>,
/// <c>destination</c>…) a textom, ktory by INISS poslal na tabulu (vratane striedanych casti oddelenych <c>#</c>).
/// </summary>
public sealed record XmlTableCell(string Name, string Text);
