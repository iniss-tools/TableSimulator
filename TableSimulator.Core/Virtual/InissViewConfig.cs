using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace TableSimulator.Core.Virtual;

/// <summary>Definicia tabule INISSView - jeden kluc s cislom v <c>appSettings</c> suboru <c>Web.config</c>.</summary>
/// <param name="Number">cislo tabule (<c>Tabule.aspx?t=</c>)</param>
/// <param name="Name">parameter 1 - kluc tabule z TPhysic</param>
/// <param name="Title">parameter 2 - popis pre zoznam (predvolene nazov)</param>
/// <param name="Xml">parameter 3 - subor XML bez pripony (predvolene nazov)</param>
/// <param name="Template">parameter 4 - sablona XSL bez pripony (predvolene nazov)</param>
/// <param name="RowCount">parameter 5 pred lomkou - pocet zaznamov; null = vsetky</param>
/// <param name="RowStart">parameter 5 za lomkou - poradie prveho zaznamu (od 1)</param>
/// <param name="Separator">parameter 6 - text namiesto <c>#</c>; prazdny = casti sa striedaju</param>
/// <param name="Parameter">parameter 7 - atribut <c>x</c> pre sablonu</param>
public sealed record InissViewTable(int Number, string Name, string Title, string Xml, string Template, int? RowCount, int RowStart, string Separator, string Parameter);

/// <summary>
/// Nastavenie INISSView zo suboru <c>Web.config</c>: definicie tabul a priecinky s XML. Hodnoty sa spracuju ako
/// v INISSView: parametre oddeluje <c>TabInfoSeparator</c> (predvolene <c>;</c>), apostrofy sa vynechaju (oddelovac
/// v nich moze mat medzery), prazdny nazov suboru XML a sablony je nazov tabule, duplicitne cislo sa vynecha.
/// </summary>
/// <param name="Tables">definicie tabul v poradi suboru</param>
/// <param name="XmlFolders">priecinky s XML tabul (<c>TabXmlDir</c>, druhy za bodkociarkou)</param>
/// <param name="Duplicates">cisla, ktore subor definuje viackrat (plati prva definicia)</param>
public sealed record InissViewConfig(IReadOnlyList<InissViewTable> Tables, IReadOnlyList<string> XmlFolders, IReadOnlyList<int> Duplicates)
{
    private const int ParameterCount = 7;

    /// <summary>Priecinok sablon XSL (<c>TabXslDir</c>); prazdny = nenastaveny.</summary>
    public string XslFolder { get; init; } = "";

    /// <summary>
    /// Nastavenie skriptu stranok tabul - kluce <c>Jscript.*</c> a <c>JS.*</c> ako meno premennej (za bodkou) a hodnota,
    /// v poradi suboru; INISSView ich vypise za stranku ako <c>meno=hodnota;</c>.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> Script { get; init; } = [];

    /// <summary>Nastavenie z obsahu suboru <c>Web.config</c>.</summary>
    /// <exception cref="XmlException">subor nie je platne XML</exception>
    public static InissViewConfig Parse(string webConfig)
    {
        ArgumentNullException.ThrowIfNull(webConfig);
        var root = XDocument.Parse(webConfig).Root;
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        foreach (var add in root?.Elements().Where(e => IsNamed(e, "appSettings")).SelectMany(e => e.Elements()).Where(e => IsNamed(e, "add")) ?? [])
        {
            if (add.Attribute("key")?.Value is not { Length: > 0 } key)
                continue;
            if (!settings.ContainsKey(key))
                order.Add(key);
            settings[key] = add.Attribute("value")?.Value ?? "";
        }

        var separators = settings.GetValueOrDefault("TabInfoSeparator") is { Length: > 0 } s ? s.ToCharArray() : [';'];
        var tables = new List<InissViewTable>();
        var numbers = new HashSet<int>();
        var duplicates = new List<int>();
        foreach (var key in order)
        {
            if (Number(key) is not { } number || settings[key] is not { Length: > 0 } value)
                continue;
            if (!numbers.Add(number))
            {
                duplicates.Add(number);
                continue;
            }

            tables.Add(Table(number, value, separators));
        }

        var folders = (settings.GetValueOrDefault("TabXmlDir") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var script = order.Where(IsScriptKey).Select(k => new KeyValuePair<string, string>(k[(k.IndexOf('.', StringComparison.Ordinal) + 1)..], settings[k])).ToList();
        return new InissViewConfig(tables, folders, duplicates) { XslFolder = (settings.GetValueOrDefault("TabXslDir") ?? "").Trim(), Script = script };
    }

    private static bool IsNamed(XElement element, string name) => string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase);

    private static bool IsScriptKey(string key) =>
        key.StartsWith("JSCRIPT.", StringComparison.OrdinalIgnoreCase) || key.StartsWith("JS.", StringComparison.OrdinalIgnoreCase);

    /// <summary>Cislo tabule z kluca: cele cislo alebo <c>TAB…</c> s cislicami; ine kluce (aj <c>Jscript.*</c>) null.</summary>
    private static int? Number(string key)
    {
        var upper = key.ToUpperInvariant();
        if (IsScriptKey(key))
            return null;
        if (!upper.StartsWith("TAB", StringComparison.Ordinal) && !int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return null;
        var digits = new string(key.Where(char.IsAsciiDigit).ToArray());
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    private static InissViewTable Table(int number, string value, char[] separators)
    {
        var parts = value.Replace("'", "", StringComparison.Ordinal).Split(separators);
        string Part(int i) => i < parts.Length ? parts[i] : "";
        var name = Part(0).Trim();
        string OrName(string text) => text.Length > 0 ? text : name;

        int? count = null;
        var start = 1;
        var rows = Part(4).Trim().Split('/');
        if (int.TryParse(rows[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) && c > 0)
            count = c;
        if (rows.Length > 1 && int.TryParse(rows[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
            start = Math.Max(r, 1);

        return new InissViewTable(number, name, OrName(Part(1).Trim()), WithoutExtension(OrName(Part(2).Trim()), ".xml"),
            WithoutExtension(OrName(Part(3).Trim()), ".xsl"), count, start, Part(5), Part(ParameterCount - 1).Trim());
    }

    private static string WithoutExtension(string file, string extension) =>
        file.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? file[..^extension.Length] : file;
}
