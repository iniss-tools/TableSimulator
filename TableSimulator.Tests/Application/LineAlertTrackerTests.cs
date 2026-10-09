using TableSimulator.Application.Alerts;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Kedy oznamit vypadok linky: az po cakani, navrat len po oznamenom vypadku a pri znova pripojenom INISSe, viac
/// liniek v jednom oznameni; texty oznameni.
/// </summary>
[TestClass]
public class LineAlertTrackerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    private static readonly AlertSettings Settings = new() { Enabled = true, DelaySeconds = 30 };

    private static AlertLineState Line(int number, string status, int since = 0, string? detail = null) =>
        new(number, $"L{number}", status, detail, Start.AddSeconds(since));

    private static DateTimeOffset At(int seconds) => Start.AddSeconds(seconds);

    [TestMethod]
    public void Vypadok_AzPoCakaniAJedenRaz()
    {
        var tracker = new LineAlertTracker();
        tracker.Update([Line(1, "connected")], At(0), Settings);

        Assert.IsEmpty(tracker.Update([Line(1, "lost", 10)], At(12), Settings).Outages);
        var due = tracker.Update([Line(1, "lost", 10)], At(40), Settings).Outages;
        Assert.AreEqual(1, due.Single().Number);
        Assert.AreEqual(At(10), due[0].Since);
        Assert.IsEmpty(tracker.Update([Line(1, "lost", 10)], At(50), Settings).Outages);

        var recovered = tracker.Update([Line(1, "connected", 55)], At(56), Settings).Recoveries;
        Assert.AreEqual(1, recovered.Single().Number);
    }

    [TestMethod]
    public void KratkyVypadok_ARestartLinky_BezOznamenia()
    {
        var tracker = new LineAlertTracker();
        tracker.Update([Line(1, "lost", 0)], At(5), Settings);
        var back = tracker.Update([Line(1, "connected", 8)], At(8), Settings);
        Assert.IsEmpty(back.Outages);
        Assert.IsEmpty(back.Recoveries);

        // linka nebezi, oznami sa; po zmene koncoveho bodu len caka - to nie je navrat INISSu
        Assert.HasCount(1, tracker.Update([Line(2, "error", 0, "port obsadeny")], At(31), Settings).Outages);
        Assert.IsEmpty(tracker.Update([Line(2, "listening", 40)], At(41), Settings).Recoveries);
        Assert.IsEmpty(tracker.Update([Line(2, "connected", 50)], At(51), Settings).Recoveries);
    }

    [TestMethod]
    public void ViacLiniekNaraz_JednoOznamenie_BezNavratuPodlaNastavenia()
    {
        var tracker = new LineAlertTracker();
        var settings = Settings with { DelaySeconds = 0, Recovery = false };

        var due = tracker.Update([Line(1, "lost"), Line(2, "error"), Line(3, "connected")], At(1), settings).Outages;
        CollectionAssert.AreEqual(new[] { 1, 2 }, due.Select(l => l.Number).ToArray());
        Assert.IsEmpty(tracker.Update([Line(1, "connected", 5), Line(2, "connected", 5), Line(3, "connected")], At(6), settings).Recoveries);
    }

    [TestMethod]
    public void Texty_SlovenskyACesky()
    {
        AlertLine[] lines = [new(1, "Hala", "lost", null, Start), new(2, "", "error", "Address already in use", Start)];

        var sk = AlertMessage.Create(AlertKind.Outage, lines, Start, "PC1", "sk", TimeZoneInfo.Utc);
        var cs = AlertMessage.Create(AlertKind.Recovery, [lines[0]], Start, "PC1", "cs", TimeZoneInfo.Utc);

        Assert.AreEqual("TableSimulator: Výpadok liniek: 2", sk.Subject);
        StringAssert.Contains(sk.Text, "Linka 1 – Hala: INISS sa odpojil (od 9. 10. 2026 12:00:00)");
        StringAssert.Contains(sk.Text, "Linka 2: linka nebeží – Address already in use");
        StringAssert.Contains(sk.Text, "na počítači PC1");
        Assert.AreEqual("outage", sk.Event);
        Assert.AreEqual("TableSimulator: Znovu funguje: Linka 1 – Hala", cs.Subject);
        StringAssert.Contains(cs.Text, "INISS je znovu připojen");
    }
}
