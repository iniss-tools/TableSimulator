using System.Text;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Znaky tabul Elektrocas: Windows-1250, pri LCD1 su <c>0x84</c> a <c>0x85</c> sipky « a ». Tabule LCD maju navyse
/// cervene pole <c>0x7F</c> a velke pismena druhu vlaku <c>0x81</c>, <c>0x83</c>, <c>0x88</c>, <c>0x90</c> (R, I, E, C)
/// - posielaju ich pravidla TabTab stanice, INISS ich nepreklada.
/// </summary>
public static class ElektrocasCharset
{
    private const byte ArrowLeft = 0x84;
    private const byte ArrowRight = 0x85;

    /// <summary>Cervene pole pri vlakoch vyssej kategorie (pravidlo <c>PRIZNAK</c>).</summary>
    public const byte RedField = 0x7F;

    /// <summary>Cervene pole v texte tabule.</summary>
    public const char RedFieldText = '■';

    /// <summary>Velke pismena druhu vlaku ("velke fonty" LCD) - kody mimo CP1250.</summary>
    private static readonly Dictionary<byte, char> BigLetters = new() { [0x81] = 'R', [0x83] = 'I', [0x88] = 'E', [0x90] = 'C' };

    /// <summary>Vyrobcovia LCD tabul Elektrocas.</summary>
    private static readonly string[] LcdManufacturers = ["LCD", "LCD1", "ERP"];

    /// <summary>Text znakov podla vyrobcu tabule.</summary>
    public static string Decode(ReadOnlySpan<byte> codes, string manufacturer)
    {
        var text = new StringBuilder(Encodings.Win1250.GetString(codes));
        if (!LcdManufacturers.Contains(manufacturer, StringComparer.OrdinalIgnoreCase) || text.Length != codes.Length)
            return text.ToString();

        var lcd1 = string.Equals(manufacturer, "LCD1", StringComparison.OrdinalIgnoreCase);
        for (var i = 0; i < codes.Length; i++)
        {
            if (codes[i] == RedField)
                text[i] = RedFieldText;
            else if (BigLetters.TryGetValue(codes[i], out var letter))
                text[i] = letter;
            else if (lcd1 && codes[i] == ArrowLeft)
                text[i] = '«';
            else if (lcd1 && codes[i] == ArrowRight)
                text[i] = '»';
        }

        return text.ToString();
    }
}
