using System.Globalization;

namespace TableSimulator.Core.Recording;

/// <summary>Poradie tabul podla linky a adresy (<c>2-10</c> za <c>2-9</c>).</summary>
public sealed class BoardIdComparer : IComparer<string>
{
    public static BoardIdComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        var a = Parts(x);
        var b = Parts(y);
        var line = a.Line.CompareTo(b.Line);
        return line != 0 ? line : a.Address != b.Address ? a.Address.CompareTo(b.Address) : string.CompareOrdinal(x, y);
    }

    private static (int Line, int Address) Parts(string? id)
    {
        var parts = (id ?? "").Split('-');
        return parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var line)
                                 && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var address)
            ? (line, address)
            : (int.MaxValue, int.MaxValue);
    }
}
