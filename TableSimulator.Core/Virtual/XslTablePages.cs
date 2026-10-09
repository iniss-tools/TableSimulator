using System.Collections.Concurrent;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Virtual;

/// <summary>Co z XML tabule ukazat (ako parametre 5 az 7 definicie tabule INISSView).</summary>
/// <param name="RowCount">pocet zaznamov; null = vsetky</param>
/// <param name="RowStart">poradie prveho zaznamu (od 1)</param>
/// <param name="Separator">text namiesto <c>#</c> v stanici a smere; prazdny = casti sa striedaju (skriptom stranky)</param>
/// <param name="Parameter">atribut <c>x</c> korena pre sablonu; prazdny = bez neho</param>
public sealed record XslTableOptions(int? RowCount, int RowStart, string Separator, string Parameter);

/// <summary>
/// Stranka tabule zo sablony XSL INISSView - ako <c>Tabule.aspx</c>: XML tabule s nahradenym <c>#</c> v stanici
/// a smere (okrem textu konciaceho <c>#</c> - ten blika), atributom <c>x</c> a vybranymi zaznamami prezenie sablonou
/// (XSLT 1.0 bez skriptov a funkcie <c>document()</c>) a za stranku prida skript s nastavenim <c>Jscript.*</c>
/// z <c>Web.config</c>. Skompilovanu sablonu si pamata, kym sa jej subor nezmeni.
/// </summary>
public sealed class XslTablePages
{
    private const int ReadAttempts = 10;
    private static readonly TimeSpan ReadRetry = TimeSpan.FromMilliseconds(50);

    private readonly ConcurrentDictionary<string, (DateTime Modified, XslCompiledTransform Transform)> _templates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Stranka HTML.</summary>
    /// <param name="xmlPath">XML tabule</param>
    /// <param name="xslPath">sablona</param>
    /// <param name="options">vyber zaznamov, oddelovac a atribut x</param>
    /// <param name="script">nastavenie skriptu stranky (meno premennej a hodnota JavaScriptu) v poradi Web.config</param>
    /// <exception cref="XsltException">chybna sablona</exception>
    /// <exception cref="XmlException">chybne XML alebo sablona</exception>
    /// <exception cref="IOException">subor sa neda citat</exception>
    public string Render(string xmlPath, string xslPath, XslTableOptions options, IReadOnlyList<KeyValuePair<string, string>> script)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(script);
        var transform = Template(xslPath);
        var document = Load(xmlPath);
        Prepare(document, options);

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using (var reader = document.CreateReader())
            transform.Transform(reader, null, writer);
        if (script.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("<!-- nastavenie skriptu podla Web.config -->");
            writer.WriteLine("<script>");
            foreach (var (name, value) in script)
                writer.WriteLine($"{name}={(value.Length > 0 ? value : "null")};");
            writer.WriteLine("</script>");
        }

        return writer.ToString();
    }

    /// <summary>XML tabule po uprave - co dostane sablona (na testy a diagnostiku).</summary>
    public static XDocument Prepare(XDocument document, XslTableOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        var elements = document.Descendants().ToList();

        if (options.Separator.Length > 0)
        {
            foreach (var element in elements.Where(e => !e.HasElements && (IsNamed(e, "destination") || IsNamed(e, "direction"))))
            {
                if (element.Value.Length > 0 && !element.Value.EndsWith('#'))
                    element.Value = element.Value.Replace("#", options.Separator, StringComparison.Ordinal);
            }
        }

        if (options.Parameter.Length > 0)
        {
            // INISSView vlozi atribut prvemu prvku T a prvemu prvku station
            elements.FirstOrDefault(e => e.Name.LocalName == "T")?.SetAttributeValue("x", options.Parameter);
            elements.FirstOrDefault(e => IsNamed(e, "station"))?.SetAttributeValue("x", options.Parameter);
        }

        if (options.RowCount is not null || options.RowStart > 1)
        {
            // zaznamy archivu su prvky t, inak row
            var rows = elements.Where(e => e.Name.LocalName == "t").ToList();
            if (rows.Count == 0)
                rows = elements.Where(e => IsNamed(e, "row")).ToList();
            var kept = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (i + 1 >= options.RowStart && (options.RowCount is not { } count || kept < count))
                    kept++;
                else
                    rows[i].Remove();
            }
        }

        return document;
    }

    private XslCompiledTransform Template(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("XSL template not found.", path);
        var modified = File.GetLastWriteTimeUtc(path);
        if (_templates.TryGetValue(path, out var cached) && cached.Modified == modified)
            return cached.Transform;

        // sablony su vo windows-1250 - kodove stranky registruje ToolsCore.Iniss
        _ = Encodings.Win1250;
        var transform = new XslCompiledTransform();
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
        using (var reader = XmlReader.Create(path, settings))
            transform.Load(reader, XsltSettings.Default, new XmlUrlResolver());
        _templates[path] = (modified, transform);
        return transform;
    }

    /// <summary>XML tabule; kym ho INISS zapisuje, skusa to znova.</summary>
    private static XDocument Load(string path)
    {
        _ = Encodings.Win1250;
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = XmlReader.Create(stream, settings);
                return XDocument.Load(reader);
            }
            catch (Exception e) when (attempt < ReadAttempts && e is IOException and not FileNotFoundException and not DirectoryNotFoundException or XmlException)
            {
                Thread.Sleep(ReadRetry);
            }
        }
    }

    private static bool IsNamed(XElement element, string name) => string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase);
}
