using System.Text;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Variant protokolu ELEN podla vyrobcu tabule (<c>MANUFACTURER_KEY</c>) - viz docs/iniss/protokoly/elen.mdx.
/// </summary>
/// <param name="Key">kluc vyrobcu ako v INISSe</param>
/// <param name="Encoding">kodovanie textu</param>
/// <param name="ExtendedFont">tabula dostava aj horny bajt cisla pisma (<c>19 h</c>)</param>
/// <param name="MaxMovingParts">najviac casti pohybliveho textu</param>
/// <param name="DefaultFont">zakladne pismo, ked INISS ziadne nenastavi</param>
/// <param name="CursorCodes">variant pozna kody polohy kurzora (ELENOLD nie)</param>
/// <param name="WideCursor">kod <c>14</c> je poloha kurzora 512-639 (ELEKON); panely ELEN ho beru ako atribut pisma</param>
/// <param name="PointSpace">server INISS2 konci riadok pismom a bajtom <c>90</c>, medzerou sirokou jeden bod (ELEKON)</param>
/// <param name="CursorUnit">bodov na jednotku polohy kurzora; ERP_ELEN ma polohu v znakoch</param>
public sealed record ElenVariant(
    string Key,
    Encoding Encoding,
    bool ExtendedFont,
    int MaxMovingParts,
    int DefaultFont,
    bool CursorCodes,
    bool WideCursor = false,
    bool PointSpace = false,
    int CursorUnit = 1)
{
    public static readonly ElenVariant Elen = new("ELEN", Encodings.Kamenicky, false, 24, 0x42, true);

    /// <summary>Firmver ELEN s kodom Windows, pozna ho len INISS2.</summary>
    public static readonly ElenVariant ElenWin = new("ELENWIN", Encodings.Win1250, false, 24, 0x42, true);

    public static readonly ElenVariant ElenOld = new("ELENOLD", Encodings.Kamenicky, false, 24, 0x42, false);
    public static readonly ElenVariant Elen10 = new("ELEN10", Encodings.Win1250, true, 64, 0x42, true);
    public static readonly ElenVariant Elen16 = new("ELEN16", Encodings.Win1250, true, 64, 0x42, true);
    public static readonly ElenVariant Elen16Kam = new("ELEN16Kam", Encodings.Kamenicky, true, 64, 0x42, true);
    public static readonly ElenVariant Elekon = new("ELEKON", Encodings.Win1250, false, 24, 0x40, true, true, true);

    /// <summary>Firmver ELEKON s kodom Kamenickych, pozna ho len INISS2.</summary>
    public static readonly ElenVariant ElekonKam = new("ELEKONKAM", Encodings.Kamenicky, false, 24, 0x40, true, true, true);

    /// <summary>Hlasic ERP na protokole ELEN, pozna ho len INISS2: pisma nedostava, kazdy znak ma sirku 1.</summary>
    public static readonly ElenVariant ErpElen = new("ERP_ELEN", Encodings.Win1250, false, 24, 0x42, true, CursorUnit: 8);

    /// <summary>Vsetky varianty v poradi dokumentacie.</summary>
    public static IReadOnlyList<ElenVariant> All { get; } = [Elen, ElenWin, ElenOld, Elen10, Elen16, Elen16Kam, Elekon, ElekonKam, ErpElen];

    /// <summary>Variant podla kluca vyrobcu (bez ohladu na velkost pismen); null = nie je ELEN.</summary>
    public static ElenVariant? Find(string? key) =>
        All.FirstOrDefault(v => string.Equals(v.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase));
}
