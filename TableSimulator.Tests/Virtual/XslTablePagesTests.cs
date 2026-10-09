using System.Xml.Xsl;
using TableSimulator.Core.Virtual;
using ToolsCore.Iniss.Tools;
using static TableSimulator.Tests.Virtual.XmlTableTests;

namespace TableSimulator.Tests.Virtual;

/// <summary>
/// Stranka tabule zo sablony XSL ako v INISSView: vyber zaznamov, oddelovac v stanici (okrem blikajuceho textu),
/// atribut x, skript z Web.config, kodovanie windows-1250 a nova sablona po zmene suboru.
/// </summary>
[TestClass]
public sealed class XslTablePagesTests
{
    private const string Template = """
        <?xml version="1.0" encoding="windows-1250"?>
        <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
        <xsl:output method="html" version="4.0" indent="no" encoding="windows-1250"/>
        <xsl:template match="/station">
        <html><head><title>Odchody</title></head>
        <body onload="initTabForm();"><h1><xsl:value-of select="@name"/>|<xsl:value-of select="@x"/></h1>
        <table><xsl:for-each select="row"><tr><td><xsl:value-of select="number"/></td><td><xsl:value-of select="destination"/></td></tr></xsl:for-each></table>
        </body></html>
        </xsl:template>
        </xsl:stylesheet>
        """;

    private string _folder = "";

    [TestInitialize]
    public void Init()
    {
        _folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_folder, true);

    private string Write(string name, byte[] content)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    private string XmlFile() => Write("Odchody.xml", Xml(Row("11:35", "7813", "Bratislava#Wien") + Row("11:52", "601", "Žilina#") + Row("12:10", "1771")));

    private string XslFile(string template = Template) => Write("Odchody_velka_tabule.xsl", Encodings.Win1250.GetBytes(template));

    [TestMethod]
    public void Stranka_VyberOddelovacAtributASkript()
    {
        var html = new XslTablePages().Render(XmlFile(), XslFile(), new XslTableOptions(2, 1, ", ", "odkaz"), [new("iNextString", "1500"), new("bSeconds", "")]);

        StringAssert.Contains(html, "<h1>Dolné Mesto|odkaz</h1>");
        StringAssert.Contains(html, "<td>Bratislava, Wien</td>");
        // text konciaci # blika - oddelovac ho nemeni
        StringAssert.Contains(html, "<td>Žilina#</td>");
        Assert.DoesNotContain("1771", html);
        StringAssert.Contains(html, "<body onload=\"initTabForm();\">");
        StringAssert.Contains(html, "<script>");
        StringAssert.Contains(html, "iNextString=1500;");
        StringAssert.Contains(html, "bSeconds=null;");
    }

    [TestMethod]
    public void Stranka_PoradiePrvehoZaznamuBezOddelovaca()
    {
        var html = new XslTablePages().Render(XmlFile(), XslFile(), new XslTableOptions(null, 2, "", ""), []);

        Assert.DoesNotContain("7813", html);
        StringAssert.Contains(html, "<td>601</td>");
        StringAssert.Contains(html, "<td>1771</td>");
        Assert.DoesNotContain("<script>", html);
        StringAssert.Contains(html, "<h1>Dolné Mesto|</h1>");
    }

    [TestMethod]
    public void Sablona_PoZmeneSuboruNova()
    {
        var pages = new XslTablePages();
        var xml = XmlFile();
        var xsl = XslFile();
        StringAssert.Contains(pages.Render(xml, xsl, new XslTableOptions(null, 1, "", ""), []), "<title>Odchody</title>");

        XslFile(Template.Replace("<title>Odchody</title>", "<title>Príchody</title>", StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(xsl, DateTime.UtcNow.AddMinutes(1));

        StringAssert.Contains(pages.Render(xml, xsl, new XslTableOptions(null, 1, "", ""), []), "<title>Príchody</title>");
    }

    [TestMethod]
    public void Sablona_ChybnaVyhodiVynimku()
    {
        var xsl = XslFile(Template.Replace("xsl:value-of select=\"number\"", "xsl:value-of select=\"number(\"", StringComparison.Ordinal));

        Assert.Throws<XsltException>(() => new XslTablePages().Render(XmlFile(), xsl, new XslTableOptions(null, 1, "", ""), []));
    }
}
