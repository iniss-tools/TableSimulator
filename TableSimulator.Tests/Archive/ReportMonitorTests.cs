using System.Text;
using TableSimulator.Application.Archive;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Archive;

namespace TableSimulator.Tests.Archive;

/// <summary>
/// Monitor hlaseni: priebezne citanie archivu hlaseni (len pridane cele riadky, legenda od zaciatku, novy den a skrateny
/// subor od zaciatku) a spravy prehliadacom.
/// </summary>
[TestClass]
public sealed class ReportMonitorTests
{
    private static readonly Encoding Win1250 = ArchiveReader.Encoding;

    private sealed class TestLive : ILiveNotifier
    {
        public List<ReportsMessage> Messages { get; } = [];

        public void Publish(LiveMessage message)
        {
            if (message is ReportsMessage reports)
                Messages.Add(reports);
        }
    }

    private DirectoryInfo _dir = null!;

    [TestInitialize]
    public void Init() => _dir = Directory.CreateTempSubdirectory("tsrep");

    [TestCleanup]
    public void Cleanup() => _dir.Delete(true);

    private static string Report(string time, string name, string train = "") =>
        $"""<H d="2026-10-10" c="{time}" u="hlásenie" n="{name}" s="Dolné Mesto"{(train.Length > 0 ? $" t=\"Os\" v=\"{train}\"" : "")} vyl="12"/>""";

    private string Append(string file, string text)
    {
        var path = Path.Combine(_dir.FullName, file);
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        stream.Write(Win1250.GetBytes(text));
        return path;
    }

    private ArchiveDay Day(string file) =>
        ArchiveFiles.Days([_dir.FullName], ArchiveKind.Reports).Single(d => Path.GetFileName(d.Path) == file);

    [TestMethod]
    public void Citanie_LenPridaneCeleRiadkyALegenda()
    {
        Append("REP2026-10-10.XML", "; 10.10.2026 06:00:00 Legenda (platná od teraz):\r\n;  V12 Výluka Lehota - Brod\r\n"
                                     + Report("06:01:00", "Príchod", "7813") + "\r\n" + Report("06:02:00", "Odchod", "7813") + "\r\n<H c=\"06:03");
        var tail = new ReportTail();

        var first = tail.Update(Day("REP2026-10-10.XML"));
        Assert.IsTrue(first.Reset);
        CollectionAssert.AreEqual(new[] { "Príchod", "Odchod" }, first.Added.Select(e => e.Name).ToArray());
        Assert.AreEqual("V12 Výluka Lehota - Brod", first.Added[0].Details.Single(d => d.Key == "vyl").Value);

        // nezmeneny subor sa necita, rozpisany riadok sa docita, ked ho INISS dokonci
        Assert.IsTrue(tail.Update(Day("REP2026-10-10.XML")).IsEmpty);
        Append("REP2026-10-10.XML", ":00\" u=\"stav\" n=\"Výluka\"/>\r\nnie xml\r\n" + Report("06:04:00", "Info") + "\r\n");
        var next = tail.Update(Day("REP2026-10-10.XML"));
        Assert.IsFalse(next.Reset);
        CollectionAssert.AreEqual(new[] { "Výluka", "Info" }, next.Added.Select(e => e.Name).ToArray());
        Assert.AreEqual(4, tail.Entries.Count);
        Assert.AreEqual(1, tail.Skipped);
        Assert.AreEqual("V12 Výluka Lehota - Brod", next.Added[1].Details.Single(d => d.Key == "vyl").Value);
    }

    [TestMethod]
    public void Citanie_NovyDenASkratenySubor()
    {
        Append("REP2026-10-09.XML", Report("23:59:00", "Včera") + "\r\n");
        var tail = new ReportTail();
        tail.Update(Day("REP2026-10-09.XML"));

        Append("REP2026-10-10.XML", Report("00:01:00", "Dnes") + "\r\n");
        var today = tail.Update(Day("REP2026-10-10.XML"));
        Assert.IsTrue(today.Reset);
        CollectionAssert.AreEqual(new[] { "Dnes" }, tail.Entries.Select(e => e.Name).ToArray());

        File.WriteAllText(Path.Combine(_dir.FullName, "REP2026-10-10.XML"), "", Win1250);
        Append("REP2026-10-10.XML", Report("00:02:00", "Znova") + "\r\n");
        var rewritten = tail.Update(Day("REP2026-10-10.XML"));
        Assert.IsTrue(rewritten.Reset);
        CollectionAssert.AreEqual(new[] { "Znova" }, tail.Entries.Select(e => e.Name).ToArray());

        Assert.IsTrue(tail.Update(null).Reset);
        Assert.IsNull(tail.Day);
        Assert.IsEmpty(tail.Entries);
    }

    [TestMethod]
    public void Monitor_NajnovsiDenSpravyAOdNajnovsieho()
    {
        var live = new TestLive();
        var monitor = new ReportMonitor(live);
        Append("REP2026-10-09.XML", Report("12:00:00", "Včera") + "\r\n");
        Append("REP2026-10-10.XML", Report("06:01:00", "Prvé") + "\r\n" + Report("06:02:00", "Druhé") + "\r\n");

        monitor.Poll([_dir.FullName]);
        monitor.Poll([_dir.FullName]);
        Append("REP2026-10-10.XML", Report("06:03:00", "Tretie", "601") + "\r\n");
        monitor.Poll([_dir.FullName]);

        Assert.HasCount(2, live.Messages);
        Assert.IsTrue(live.Messages[0].Reset);
        Assert.AreEqual((new DateOnly(2026, 10, 10), 2), (live.Messages[0].Date!.Value, live.Messages[0].Total));
        Assert.AreEqual("Tretie", live.Messages[1].Added.Single().Name);
        Assert.AreEqual(3, live.Messages[1].Total);

        var snapshot = monitor.Snapshot(2);
        Assert.AreEqual("REP2026-10-10.XML", snapshot.File);
        Assert.AreEqual(3, snapshot.Total);
        CollectionAssert.AreEqual(new[] { "Tretie", "Druhé" }, snapshot.Entries.Select(e => e.Name).ToArray());
    }
}
