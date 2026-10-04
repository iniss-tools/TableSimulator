using TableSimulator.Core.Monitor;

namespace TableSimulator.Tests.Monitor;

/// <summary>
/// Subory programu ELEN Monitor: typ panelu (.cfg), konfiguracia panelu (rwp_*.bin), projekt (.prj), Monitor.INI.
/// </summary>
[TestClass]
public class MonitorFilesTests
{
    private const string Cfg = """
        [SIM]
        Name=Sip SDO 5x320 YC ODCH A X3 SK
        Font=10_WIN1250_rwp_070125.bin
        Config=rwp_10_320_5r_SDO.bin
        Velkost=4
        Stlpce=320
        [HEAD]
        Vyska=35
        SimText=1
        x0=80
        y0=1
        h0=10
        txt0=ODCHODY / DEPARTURES
        x1=5
        y1=20
        h1=4
        txt1=ODCHOD
        """;

    /// <summary>Hlavicka konfiguracie panelu 10×320, 5 riadkov, jednofarebny, s hodinami, FlashON 10, FlashOFF 5.</summary>
    private static byte[] Config(int color = 0, int clock = 0x0C)
    {
        var data = new byte[MonitorPanelConfig.Size];
        byte[] head = [0x03, 0x05, 0x0A, 0x01, 0x40, 0x01, 0x00, 0x00, 0x40, (byte)color, 0x02, 0x00, 0x05, 0x3F, 0x48, 0x00, 0x03];
        head.CopyTo(data, 0);
        data[25] = 10;
        data[26] = 5;
        data[27] = (byte)clock;
        return data;
    }

    [TestMethod]
    public void TypPanelu_HlavickaLenPrvychSimText()
    {
        var panel = MonitorPanel.Parse("p.cfg", Cfg);

        Assert.AreEqual("Sip SDO 5x320 YC ODCH A X3 SK", panel.Name);
        Assert.AreEqual("rwp_10_320_5r_SDO.bin", panel.ConfigFile);
        Assert.AreEqual(320, panel.Columns);
        Assert.AreEqual(35, panel.HeaderHeight);
        Assert.AreEqual(new MonitorHeaderText(80, 1, 10, "ODCHODY / DEPARTURES"), panel.HeaderTexts.Single());
    }

    [TestMethod]
    public void Konfiguracia_RozmerFarbaHodinyBlikanie()
    {
        var config = MonitorPanelConfig.Read(Config())!;

        Assert.AreEqual(5, config.Rows);
        Assert.AreEqual(10, config.RowHeight);
        Assert.AreEqual(320, config.Width);
        Assert.AreEqual(0, config.Color);
        Assert.IsTrue(config.Clock);
        Assert.AreEqual(10, config.FlashOn);
        Assert.AreEqual(5, config.FlashOff);
        Assert.IsNull(MonitorPanelConfig.Read(new byte[100]));
    }

    [TestMethod]
    public void Profil_ZTypuPaneluAKonfiguracie()
    {
        var profile = MonitorFolder.ToProfile(MonitorPanel.Parse("p.cfg", Cfg), MonitorPanelConfig.Read(Config()));

        Assert.AreEqual(320, profile.Width);
        Assert.AreEqual(5, profile.Rows);
        Assert.AreEqual(1000, profile.BlinkOnMs);
        Assert.AreEqual(500, profile.BlinkOffMs);
        Assert.IsTrue(profile.Monochrome);
        Assert.AreEqual("10_WIN1250_rwp_070125.bin", profile.FontFile);
        Assert.AreEqual(35, profile.Header!.Height);
        // hodiny 45 bodov od praveho okraja, 4 body zhora, 8 bodov vysoke
        Assert.AreEqual(275, profile.Header.Clock!.X);
        Assert.AreEqual(4, profile.Header.Clock.Y);
        Assert.AreEqual(8, profile.Header.Clock.Height);
    }

    [TestMethod]
    public void Profil_BezHodinAZamenenaFarba()
    {
        var profile = MonitorFolder.ToProfile(MonitorPanel.Parse("p.cfg", Cfg), MonitorPanelConfig.Read(Config(color: 2, clock: 0)));

        Assert.IsNull(profile.Header!.Clock);
        Assert.IsFalse(profile.Monochrome);
        Assert.IsTrue(profile.SwapRedGreen);
    }

    [TestMethod]
    public void Projekt_DvojitaAdresaARezim()
    {
        const string prj = "Sip HDI\r\nHala\r\n5;6\r\ndisplay\r\nyes\r\na.cfg\r\nELMB\r\n\r\n9\r\nmonitor\r\nno\r\nb.cfg\r\n";

        var entries = MonitorProject.Parse(prj);

        Assert.HasCount(2, entries);
        CollectionAssert.AreEqual(new[] { 5, 6 }, entries[0].Addresses.ToArray());
        Assert.AreEqual("Hala", entries[0].Location);
        Assert.IsTrue(entries[0].Open);
        Assert.IsTrue(entries[1].Monitor);
        Assert.AreEqual("b.cfg", entries[1].ConfigFile);
    }

    [TestMethod]
    public void Vzhlad_FarbyWindowsBgr()
    {
        var look = MonitorLook.Parse("[SIM]\r\nFontName=Arial\r\nFontColor=16777215\r\nHeaderColor=8388608\r\nDisplayColor=4276545\r\n");

        Assert.AreEqual(new MonitorLook("Arial", "#ffffff", "#000080", "#414141"), look);
    }
}
