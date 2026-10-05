using TableSimulator.Core.Archive;

namespace TableSimulator.Tests.Archive;

/// <summary>Citanie dennych archivov INISSu: zaznamy tabul a hlaseni, legenda, poskodene a textove riadky, vyber.</summary>
[TestClass]
public sealed class ArchiveReaderTests
{
    /// <summary>Riadky suboru v kodovani windows-1250 (ako ich zapisuje INISS).</summary>
    internal static byte[] ArchiveBytes(params string[] lines) => ArchiveReader.Encoding.GetBytes(string.Concat(lines.Select(l => l + "\r\n")));

    private static ArchiveContent Tables(params string[] lines) => ArchiveReader.ReadTables(new MemoryStream(ArchiveBytes(lines)));

    private static ArchiveContent Reports(params string[] lines) => ArchiveReader.ReadReports(new MemoryStream(ArchiveBytes(lines)));

    internal const string TrainLine =
        """<T d="2026-10-04" c="14:03:22" s="Dolné Mesto" t="Os" v="7813" jm="Rozkvet" dt="2026-10-03"><t><n>NAST1</n><n>NAST2</n><c n="Čas" s="14:05"/><c n="Smer" s="Horná Ves"/><c n="Poznámka"/></t><t><n>ODCH</n><c n="Čas" s="14:05"/></t></T>""";

    [TestMethod]
    public void ReadTables_ZaznamVlaku_TabuleSRovnakymObsahom()
    {
        var content = Tables(TrainLine);

        Assert.AreEqual(0, content.Skipped);
        var entry = content.Tables.Single();
        Assert.AreEqual(new TimeOnly(14, 3, 22), entry.Time);
        Assert.AreEqual("Dolné Mesto", entry.Station);
        Assert.AreEqual(new ArchiveTrain("Os", "7813", "Rozkvet", new DateOnly(2026, 10, 3)), entry.Train);
        Assert.HasCount(2, entry.Tables);
        CollectionAssert.AreEqual(new[] { "NAST1", "NAST2" }, entry.Tables[0].Names.ToArray());
        CollectionAssert.AreEqual(new[] { new TableArchiveColumn("Čas", "14:05"), new TableArchiveColumn("Smer", "Horná Ves"), new TableArchiveColumn("Poznámka", "") },
            entry.Tables[0].Columns.ToArray());
        Assert.AreEqual("", entry.Tables[0].Logical);
    }

    [TestMethod]
    public void ReadTables_InformacnyText_LogickaTabulaARiadok()
    {
        var content = Tables(
            """<T d="2026-10-04" c="08:00:01" s="Dolné Mesto"><tx>Vlak 7813 mešká</tx><t lt="INFO" r="2"><n r="2">PERON1</n></t></T>""",
            """<T d="2026-10-04" c="08:30:00" s="Dolné Mesto"><tx></tx><t lt="INFO" r="2"><n r="2">PERON1</n></t></T>""");

        var shown = content.Tables[0];
        Assert.IsNull(shown.Train);
        Assert.AreEqual("Vlak 7813 mešká", shown.Text);
        Assert.AreEqual("INFO", shown.Tables[0].Logical);
        Assert.AreEqual("2", shown.Tables[0].Row);
        CollectionAssert.AreEqual(new[] { "PERON1" }, shown.Tables[0].Names.ToArray());
        Assert.IsEmpty(shown.Tables[0].Columns);
        Assert.AreEqual("", content.Tables[1].Text);
    }

    [TestMethod]
    public void ReadTables_NeuplnyPoslednyRiadok_PreskociAZarata()
    {
        var content = Tables(TrainLine, """<T d="2026-10-04" c="14:04:00" s="Dolné Mesto" t="R" v="601"><t><n>NAST1""");

        Assert.HasCount(1, content.Tables);
        Assert.AreEqual(1, content.Skipped);
    }

    [TestMethod]
    public void ReadReports_PoliaZaznamu()
    {
        var entry = Reports(
                """<H d="2026-10-04" c="14:02:10" u="hlásenie" n="Príchod" a="true" s="Dolné Mesto" t="Os" v="7813" jm="Rozkvet" kp="2" ko="3" zp="5" zo="10" x="Osobný vlak číslo 7813 prichádza" e="OK"/>""")
            .Reports.Single();

        Assert.AreEqual(new TimeOnly(14, 2, 10), entry.Time);
        Assert.AreEqual("hlásenie", entry.Kind);
        Assert.AreEqual("Príchod", entry.Name);
        Assert.IsTrue(entry.Auto);
        Assert.AreEqual(new ArchiveTrain("Os", "7813", "Rozkvet", null), entry.Train);
        Assert.AreEqual("2", entry.ArrivalTrack);
        Assert.AreEqual("3", entry.DepartureTrack);
        Assert.AreEqual(5, entry.ArrivalDelay);
        Assert.AreEqual(10, entry.DepartureDelay);
        Assert.AreEqual("Osobný vlak číslo 7813 prichádza", entry.Text);
        Assert.AreEqual("OK", entry.Result);
        Assert.IsEmpty(entry.Details);
    }

    [TestMethod]
    public void ReadReports_VylukaAOdklon_TextZLegendy()
    {
        var entry = Reports(
                "; 04.10.2026 00:00:05 Legenda (platná od teraz):",
                ";  V12 Dolné Mesto - Horná Ves, jedna koľaj",
                ";  O3 cez Novú Ves",
                """<H d="2026-10-04" c="09:00:00" u="stav" n="Výluka" s="Dolné Mesto" t="R" v="601" vylo="7" vyl="12" od="3"/>""")
            .Reports.Single();

        CollectionAssert.AreEqual(
            new[]
            {
                new KeyValuePair<string, string>("vylo", "V7"),
                new KeyValuePair<string, string>("vyl", "V12 Dolné Mesto - Horná Ves, jedna koľaj"),
                new KeyValuePair<string, string>("od", "O3 cez Novú Ves"),
            },
            entry.Details.ToArray());
        Assert.IsNull(entry.ArrivalDelay);
    }

    [TestMethod]
    public void ReadReports_NezakodovanyAmpersand_Opravi()
    {
        var entry = Reports("""<H d="2026-10-04" c="10:00:00" u="hlásenie" n="Info" s="Dolné Mesto" x="Kúpte si lístok v pokladni & automate"/>""").Reports.Single();

        Assert.AreEqual("Kúpte si lístok v pokladni & automate", entry.Text);
        Assert.IsNull(entry.Train);
    }

    [TestMethod]
    public void ReadReports_Iniss2_CasSoZlomkamiADalsieAtributy()
    {
        var entry = Reports("""<H d="2019-08-04" c="10:15:30.1234567+02:00" u="Hlášení" n="Odjezd" s="Horní Ves" t="Os" v="3101" kp="1" zp="20" zcp="20" k="true"/>""")
            .Reports.Single();

        Assert.AreEqual(new TimeOnly(10, 15, 30), entry.Time);
        CollectionAssert.AreEqual(new[] { new KeyValuePair<string, string>("zcp", "20"), new KeyValuePair<string, string>("k", "true") }, entry.Details.ToArray());
    }

    [TestMethod]
    public void ReadReports_TextovyTvarLogu_Preskoci()
    {
        var content = Reports(
            "<?xml version=\"1.0\" encoding=\"windows-1250\"?>",
            "04.10.2026\t10:00:00\thlásenie\tPríchod\t7813",
            """<H d="2026-10-04" c="10:01:00" u="hlásenie" n="Odchod" s="Dolné Mesto"/>""");

        Assert.HasCount(1, content.Reports);
        Assert.AreEqual(1, content.Skipped);
    }

    [TestMethod]
    [DataRow("78", null, true)]
    [DataRow("601", null, false)]
    [DataRow(null, "horna ves", true)]
    [DataRow(null, "NAST2", true)]
    [DataRow(null, "rozkvet", true)]
    [DataRow(null, "Žilina", false)]
    [DataRow("7813", "dolne", true)]
    public void ArchiveFilter_VlakATextBezDiakritiky(string? train, string? text, bool expected)
    {
        var entry = Tables(TrainLine).Tables.Single();

        Assert.AreEqual(expected, ArchiveFilter.Matches(entry, train, text));
    }

    [TestMethod]
    public void ArchiveFilter_InformacnyText_VyberPodlaVlakuHoVynecha()
    {
        var entry = Tables("""<T d="2026-10-04" c="08:00:01" s="Dolné Mesto"><tx>Vlak 7813 mešká</tx><t lt="INFO" r="2"><n r="2">PERON1</n></t></T>""").Tables.Single();

        Assert.IsFalse(ArchiveFilter.Matches(entry, "7813", null));
        Assert.IsTrue(ArchiveFilter.Matches(entry, null, "MESKA"));
    }
}
