namespace TableSimulator.Application.Designs;

/// <summary>
/// Predvoleny vzhlad tabule bez vlastneho vzhladu podla rodiny a vyrobcu (znakove tabule Elektrocas vyzeraju ako LCD,
/// APEL cervene LED). Dvojca <c>defaultDesignId</c> vo <c>web/src/render/design.ts</c> - menit oboje naraz.
/// </summary>
public static class DesignDefaults
{
    public const string Led = "elen-led";
    public const string Flaps = "listova";
    public const string Chars = "lcd-dark";
    public const string Apel = "apel-led";

    /// <summary>Vyrobcovia s cervenymi LED tabulami APEL.</summary>
    private static readonly string[] ApelManufacturers = ["APEL", "APEL_AN"];

    /// <summary>Vzhlad tabule rodiny <paramref name="family" /> (<c>elen</c>, <c>chars</c>, <c>flaps</c>…) od vyrobcu <paramref name="manufacturer" />.</summary>
    public static string For(string family, string manufacturer)
    {
        ArgumentNullException.ThrowIfNull(manufacturer);
        if (family == "flaps")
            return Flaps;
        if (ApelManufacturers.Contains(manufacturer.ToUpperInvariant()))
            return Apel;
        return family == "chars" ? Chars : Led;
    }
}
