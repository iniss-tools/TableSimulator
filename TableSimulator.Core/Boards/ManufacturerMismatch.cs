namespace TableSimulator.Core.Boards;

/// <summary>
/// INISS posiela tabuli spravy pre ineho vyrobcu, nez ma tabula v simulatore nastaveneho (napr. rozsirene pismo
/// tabuli ELEN) - tabula potom ukazuje obsah zle.
/// </summary>
/// <param name="Evidence">co na to ukazuje (kluc pre texty rozhrania, napr. <c>elen.extendedFont</c>)</param>
/// <param name="Suggested">vyrobcovia, ktorym take spravy patria</param>
public sealed record ManufacturerMismatch(string Evidence, IReadOnlyList<string> Suggested)
{
    /// <summary>Rovnake zistenie (zoznamy porovna po prvkoch).</summary>
    public bool Equals(ManufacturerMismatch? other) =>
        other is not null && Evidence == other.Evidence && Suggested.SequenceEqual(other.Suggested);

    /// <inheritdoc />
    public override int GetHashCode() => Evidence.GetHashCode(StringComparison.Ordinal);
}
