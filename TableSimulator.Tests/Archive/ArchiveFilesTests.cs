using TableSimulator.Core.Archive;

namespace TableSimulator.Tests.Archive;

/// <summary>Dni archivu v priecinkoch logov: tvar mena, druh, novsi subor z dvoch priecinkov, chybajuci priecinok.</summary>
[TestClass]
public sealed class ArchiveFilesTests
{
    private readonly List<string> _folders = [];

    private string Folder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        _folders.Add(folder);
        return folder;
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var folder in _folders)
            Directory.Delete(folder, true);
    }

    private static string Write(string folder, string file, DateTime modified)
    {
        var path = Path.Combine(folder, file);
        File.WriteAllText(path, "");
        File.SetLastWriteTime(path, modified);
        return path;
    }

    [TestMethod]
    public void Days_NovsiSuborZDvochPriecinkov_OdNajnovsieho()
    {
        var first = Folder();
        var second = Folder();
        Write(first, "TAB2026-10-03.XML", new DateTime(2026, 10, 3, 23, 59, 0));
        Write(first, "TAB2026-10-04.XML", new DateTime(2026, 10, 4, 10, 0, 0));
        var newer = Write(second, "tab2026-10-04.xml", new DateTime(2026, 10, 4, 12, 0, 0));
        Write(second, "REP2026-10-05.XML", new DateTime(2026, 10, 5, 8, 0, 0));
        Write(second, "TAB2026-10-05.bak", new DateTime(2026, 10, 5, 8, 0, 0));
        Write(second, "TABULE.XML", new DateTime(2026, 10, 5, 8, 0, 0));
        Write(second, "TAB2026-13-01.XML", new DateTime(2026, 10, 5, 8, 0, 0));

        var days = ArchiveFiles.Days([first, second, Path.Combine(first, "chyba")], ArchiveKind.Tables);

        CollectionAssert.AreEqual(new[] { new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 3) }, days.Select(d => d.Date).ToArray());
        Assert.AreEqual(newer, days[0].Path);
        Assert.AreEqual(ArchiveKind.Tables, days[0].Kind);
        Assert.AreEqual(new DateOnly(2026, 10, 5), ArchiveFiles.Days([first, second], ArchiveKind.Reports).Single().Date);
    }
}
