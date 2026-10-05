using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Virtual;

/// <summary>
/// Cita XML tabule INISSu (<c>&lt;SAVE_XML&gt;.xml</c>). Kodovanie berie z deklaracie - INISS pise windows-1250.
/// </summary>
public static class XmlTableReader
{
    /// <summary>Tvar casov v atributoch korena.</summary>
    public const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    private const string RootName = "station";
    private const string RowName = "row";

    /// <summary>Obsah tabule; null, ak subor nie je XML tabule (iny koren nez <c>station</c>).</summary>
    /// <exception cref="XmlException">poskodene alebo neuplne XML - napriklad ho INISS prave zapisuje</exception>
    public static XmlTable? Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        // kodove stranky (windows-1250 z deklaracie) registruje ToolsCore.Iniss
        _ = Encodings.Win1250;
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, IgnoreComments = true, IgnoreProcessingInstructions = true };
        using var reader = XmlReader.Create(stream, settings);
        var root = XDocument.Load(reader).Root;
        if (root is null || !string.Equals(root.Name.LocalName, RootName, StringComparison.OrdinalIgnoreCase))
            return null;

        var rows = root.Elements()
            .Where(e => string.Equals(e.Name.LocalName, RowName, StringComparison.OrdinalIgnoreCase))
            .Select(row => new XmlTableRow(row.Elements().Select(c => new XmlTableCell(c.Name.LocalName, c.Value)).ToArray()))
            .ToArray();
        return new XmlTable(Attribute(root, "name"), Attribute(root, "n"), Date(root, "date"), Date(root, "validto"), Attribute(root, "sw"), rows);
    }

    private static string Attribute(XElement element, string name) =>
        element.Attributes().FirstOrDefault(a => string.Equals(a.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static DateTime? Date(XElement element, string name) =>
        DateTime.TryParseExact(Attribute(element, name), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}
