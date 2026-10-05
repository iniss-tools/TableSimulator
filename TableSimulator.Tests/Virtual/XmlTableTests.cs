using System.Xml;
using TableSimulator.Core.Virtual;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Tests.Virtual;

/// <summary>Citanie XML tabule INISSu a vyber zaznamov pre virtualnu tabulu (ako INISSView).</summary>
[TestClass]
public sealed class XmlTableTests
{
    /// <summary>XML ako ho zapisuje INISS - windows-1250, kazdy zaznam jeden prvok row.</summary>
    internal static byte[] Xml(string rows, string validTo = "2026-09-13 11:46:30") => Encodings.Win1250.GetBytes(
        $"""
         <?xml version="1.0" encoding="windows-1250"?>
         <station validto="{validTo}" name="Dolné Mesto" date="2026-09-13 11:31:30" n="Odchody web" sw="INISS v. 3.39">
         {rows}
         </station>
         """);

    internal static string Row(string time, string number, string destination = "Horné Mesto") =>
        $"<row><time>{time}</time><type>Os</type><number>{number}</number><destination>{destination}</destination><platform>2</platform><delay></delay></row>";

    private static XmlTable Read(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return XmlTableReader.Read(stream) ?? throw new AssertFailedException("Nie je XML tabule.");
    }

    private static XmlTableRow Cells(params (string Name, string Text)[] cells) => new(cells.Select(c => new XmlTableCell(c.Name, c.Text)).ToArray());

    [TestMethod]
    public void Read_HlavickaAZaznamyVPoradiStlpcov()
    {
        var table = Read(Xml(Row("11:35", "7813", "Veľká Ves#Žilina") + Row("11:52", "601")));

        Assert.AreEqual("Dolné Mesto", table.Station);
        Assert.AreEqual("Odchody web", table.Key);
        Assert.AreEqual("INISS v. 3.39", table.Software);
        Assert.AreEqual(new DateTime(2026, 9, 13, 11, 31, 30), table.Written);
        Assert.AreEqual(new DateTime(2026, 9, 13, 11, 46, 30), table.ValidTo);
        Assert.HasCount(2, table.Rows);
        CollectionAssert.AreEqual(new[] { "time", "type", "number", "destination", "platform", "delay" }, table.Rows[0].Cells.Select(c => c.Name).ToArray());
        Assert.AreEqual("Veľká Ves#Žilina", table.Rows[0].Text("destination"));
        Assert.AreEqual("", table.Rows[0].Text("delay"));
        Assert.IsNull(table.Rows[0].Text("track"));
    }

    [TestMethod]
    public void Read_PrazdnaTabula()
    {
        var table = Read(Xml(""));

        Assert.IsEmpty(table.Rows);
        Assert.AreEqual("Dolné Mesto", table.Station);
    }

    [TestMethod]
    public void Read_InyKoren_NieJeTabula()
    {
        using var stream = new MemoryStream("<configuration><appSettings /></configuration>"u8.ToArray());

        Assert.IsNull(XmlTableReader.Read(stream));
    }

    [TestMethod]
    public void Read_NeuplnySubor_Chyba()
    {
        var data = Xml(Row("11:35", "7813"));
        using var stream = new MemoryStream(data[..(data.Length / 2)]);

        Assert.ThrowsExactly<XmlException>(() => XmlTableReader.Read(stream));
    }

    [TestMethod]
    public void IsStale_PoKoniecPlatnosti()
    {
        var table = Read(Xml(""));

        Assert.IsFalse(table.IsStale(new DateTime(2026, 9, 13, 11, 46, 30)));
        Assert.IsTrue(table.IsStale(new DateTime(2026, 9, 13, 11, 46, 31)));
    }

    [TestMethod]
    [DataRow(6, 7, 7, 12)]
    [DataRow(5, 3, 3, 7)]
    [DataRow(null, 10, 10, 15)]
    [DataRow(20, 1, 1, 15)]
    public void Apply_PocetOdPoradia(int? count, int start, int first, int last)
    {
        var rows = Enumerable.Range(1, 15).Select(i => Cells(("number", i.ToString(System.Globalization.CultureInfo.InvariantCulture)))).ToArray();

        var selected = XmlTableFilter.Apply(rows, count, start, null);

        CollectionAssert.AreEqual(Enumerable.Range(first, last - first + 1).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
            selected.Select(r => r.Text("number")).ToArray());
    }

    [TestMethod]
    public void Apply_Oddelovac_LenStanicaASmerABezBlikania()
    {
        var row = Cells(("destination", "Bratislava#Wien"), ("direction", "Trnava#Galanta"), ("text", "Pozor#Achtung"),
            ("number", "1#2"), ("destination", "Košice#"));

        var result = XmlTableFilter.Apply([row], null, 1, ", ")[0];

        CollectionAssert.AreEqual(new[] { "Bratislava, Wien", "Trnava, Galanta", "Pozor#Achtung", "1#2", "Košice#" },
            result.Cells.Select(c => c.Text).ToArray());
    }

    [TestMethod]
    public void Apply_BezOddelovaca_TextyOstanu()
    {
        var row = Cells(("destination", "Bratislava#Wien"));

        Assert.AreEqual("Bratislava#Wien", XmlTableFilter.Apply([row], null, 1, "")[0].Text("destination"));
    }
}
