using System.Xml;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Tests.Virtual;

/// <summary>Definicie tabul zo suboru Web.config INISSView - spracovanie parametrov ako v INISSView.</summary>
[TestClass]
public sealed class InissViewConfigTests
{
    private static string WebConfig(string settings) =>
        $"""
         <?xml version="1.0"?>
         <configuration>
           <appSettings>
             <add key="TabXmlDir" value="C:\INISS\Logy;\\server\iniss\Logy" />
             <add key="Jscript.iNextString" value="1500" />
             {settings}
           </appSettings>
         </configuration>
         """;

    [TestMethod]
    public void Parse_ParametreAPredvoleneHodnoty()
    {
        var config = InissViewConfig.Parse(WebConfig("""
            <add key="TabInfoSeparator" value=";" />
            <add key="1" value="Odchody web;Odchody – hala;DolneMestoO;Odjezdy_velka_tabule_12r_Praha_cerna;12" />
            <add key="3" value="Odchody web;Odchody – pokladne, 2 riadky;DolneMestoO.xml;Pokladny_2r.xsl;6/7;', '" />
            <add key="TAB7" value="Príchody web" />
            <!--<add key="8" value="Zakomentovaná;;X" />-->
            <add key="42" value="Odjezdy internet;Odjezdová tabule;5616590O;Odj_CSNK;20;;43;" />
            """));

        CollectionAssert.AreEqual(new[] { 1, 3, 7, 42 }, config.Tables.Select(t => t.Number).ToArray());
        Assert.AreEqual(new InissViewTable(1, "Odchody web", "Odchody – hala", "DolneMestoO", "Odjezdy_velka_tabule_12r_Praha_cerna", 12, 1, "", ""),
            config.Tables[0]);
        Assert.AreEqual(new InissViewTable(3, "Odchody web", "Odchody – pokladne, 2 riadky", "DolneMestoO", "Pokladny_2r", 6, 7, ", ", ""),
            config.Tables[1]);
        Assert.AreEqual(new InissViewTable(7, "Príchody web", "Príchody web", "Príchody web", "Príchody web", null, 1, "", ""), config.Tables[2]);
        Assert.AreEqual("43", config.Tables[3].Parameter);
        Assert.AreEqual(20, config.Tables[3].RowCount);
        CollectionAssert.AreEqual(new[] { @"C:\INISS\Logy", @"\\server\iniss\Logy" }, config.XmlFolders.ToArray());
        Assert.IsEmpty(config.Duplicates);
    }

    [TestMethod]
    public void Parse_PriecinokSablonANastavenieSkriptu()
    {
        var config = InissViewConfig.Parse(WebConfig("""
            <add key="TabXslDir" value=" C:\INISS\INISSview\tabs " />
            <add key="JS.bSeconds" value="false" />
            <add key="1" value="Odchody web" />
            """));

        Assert.AreEqual(@"C:\INISS\INISSview\tabs", config.XslFolder);
        CollectionAssert.AreEqual(new[] { "iNextString=1500", "bSeconds=false" }, config.Script.Select(s => $"{s.Key}={s.Value}").ToArray());
        Assert.HasCount(1, config.Tables);
    }

    [TestMethod]
    public void Parse_DuplicitneCislo_PlatiPrva()
    {
        var config = InissViewConfig.Parse(WebConfig("""
            <add key="4" value="Prvá" />
            <add key="TAB4" value="Druhá" />
            """));

        Assert.HasCount(1, config.Tables);
        Assert.AreEqual("Prvá", config.Tables[0].Name);
        CollectionAssert.AreEqual(new[] { 4 }, config.Duplicates.ToArray());
    }

    [TestMethod]
    public void Parse_VlastnyOddelovacParametrov()
    {
        var config = InissViewConfig.Parse(WebConfig("""
            <add key="TabInfoSeparator" value="|" />
            <add key="1" value="Odchody;Hala;DolneMestoO|Popis; s bodkočiarkou|X" />
            """));

        Assert.AreEqual("Odchody;Hala;DolneMestoO", config.Tables[0].Name);
        Assert.AreEqual("Popis; s bodkočiarkou", config.Tables[0].Title);
        Assert.AreEqual("X", config.Tables[0].Xml);
    }

    [TestMethod]
    public void Parse_NeplatneXml_Chyba() =>
        Assert.ThrowsExactly<XmlException>(() => InissViewConfig.Parse("<configuration><appSettings>"));
}
