using System.Text;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Znaky tabul Elektrocas: Windows-1250, pri LCD1 su <c>0x84</c> a <c>0x85</c> sipky « a ».
/// </summary>
public static class ElektrocasCharset
{
    private const byte ArrowLeft = 0x84;
    private const byte ArrowRight = 0x85;

    /// <summary>Text znakov podla vyrobcu tabule.</summary>
    public static string Decode(ReadOnlySpan<byte> codes, string manufacturer)
    {
        var text = new StringBuilder(Encodings.Win1250.GetString(codes));
        if (!string.Equals(manufacturer, "LCD1", StringComparison.OrdinalIgnoreCase) || text.Length != codes.Length)
            return text.ToString();

        for (var i = 0; i < codes.Length; i++)
        {
            if (codes[i] == ArrowLeft)
                text[i] = '«';
            else if (codes[i] == ArrowRight)
                text[i] = '»';
        }

        return text.ToString();
    }
}
