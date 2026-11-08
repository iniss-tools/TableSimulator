using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Protocols.Remote;

/// <summary>
/// Kody vyrobcov tabul (<c>MANUFACTURER_KEY</c> katalogu ako cislo), ktore riadiaci INISS posiela v <c>TCLS</c>.
/// </summary>
public static class RemoteManufacturers
{
    private static readonly string?[] Names =
        ["Adon/BUSE", "LCD", "ERS", "FERS", "ELEN", "LCD1", "ELENOLD", "ELEN10", "ELEN16", "ELEN16Kam", "APEL", "APEL_AN", "ERP", "ELEKON"];

    /// <summary>Nazov vyrobcu podla kodu alebo null.</summary>
    public static string? Name(int code) => code >= 0 && code < Names.Length ? Names[code] : null;

    /// <summary>Text polozky (fronta, nahravky, titulok) - INISS ho posiela v kodovani Windows-1250 bez ukoncenia.</summary>
    public static string Text(byte[] data) => Encodings.Win1250.GetString(data).TrimEnd('\0');

    /// <summary>Text do polozky v kodovani Windows-1250.</summary>
    public static byte[] Bytes(string text) => Encodings.Win1250.GetBytes(text);
}
