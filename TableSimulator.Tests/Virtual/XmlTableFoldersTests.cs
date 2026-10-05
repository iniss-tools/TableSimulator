using TableSimulator.Core.Virtual;
using static TableSimulator.Tests.Virtual.XmlTableTests;

namespace TableSimulator.Tests.Virtual;

/// <summary>Sledovanie priecinkov logov INISSu: vyber suborov, novsi z dvoch priecinkov, rozpisany subor, platnost.</summary>
[TestClass]
public sealed class XmlTableFoldersTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 11, 40, 0);

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

    private static string Write(string folder, string file, byte[] data, DateTime? modified = null)
    {
        var path = Path.Combine(folder, file);
        File.WriteAllBytes(path, data);
        File.SetLastWriteTime(path, modified ?? File.GetLastWriteTime(path));
        return path;
    }

    [TestMethod]
    public void Poll_TabuleBezArchivovAInychXml()
    {
        var folder = Folder();
        Write(folder, "KosiceO.xml", Xml(Row("21:07", "1764")));
        Write(folder, "TAB2026-09-13.XML", Xml(""));
        Write(folder, "REP2026-09-13.XML", "<log />"u8.ToArray());
        Write(folder, "Web.xml", "<configuration />"u8.ToArray());
        Write(folder, "KosiceO.txt", Xml(""));
        var folders = new XmlTableFolders();

        var changed = folders.Poll([folder], Now);

        CollectionAssert.AreEqual(new[] { "KosiceO" }, changed.ToArray());
        var source = folders.Find("kosiceo");
        Assert.IsNotNull(source);
        Assert.AreEqual("1764", source.Table!.Rows[0].Text("number"));
        Assert.IsNull(source.Error);
        Assert.HasCount(1, folders.Sources);
    }

    [TestMethod]
    public void Poll_NezmenenySubor_BezZmeny()
    {
        var folder = Folder();
        Write(folder, "KosiceO.xml", Xml(""));
        var folders = new XmlTableFolders();
        folders.Poll([folder], Now);

        Assert.IsEmpty(folders.Poll([folder], Now));
    }

    [TestMethod]
    public void Poll_NovsiZDvochPriecinkov_PriRovnakomCasePrvy()
    {
        var first = Folder();
        var second = Folder();
        var time = new DateTime(2026, 9, 13, 11, 0, 0);
        Write(first, "KosiceO.xml", Xml(Row("10:00", "1")), time);
        var newer = Write(second, "KosiceO.xml", Xml(Row("10:00", "2")), time.AddMinutes(1));
        var folders = new XmlTableFolders();

        folders.Poll([first, second], Now);
        Assert.AreEqual(newer, folders.Find("KosiceO")!.Path);

        File.SetLastWriteTime(newer, time);
        folders.Poll([first, second], Now);
        Assert.AreEqual("1", folders.Find("KosiceO")!.Table!.Rows[0].Text("number"));
    }

    [TestMethod]
    public void Poll_RozpisanySubor_PlatiPredoslyObsahAzPotomChyba()
    {
        var folder = Folder();
        var time = new DateTime(2026, 9, 13, 11, 0, 0);
        Write(folder, "KosiceO.xml", Xml(Row("10:00", "1")), time);
        var folders = new XmlTableFolders();
        folders.Poll([folder], Now);
        var broken = Xml(Row("10:00", "2"));
        Write(folder, "KosiceO.xml", broken[..(broken.Length / 2)], time.AddSeconds(30));

        for (var i = 1; i < XmlTableFolders.FailuresBeforeError; i++)
        {
            Assert.IsEmpty(folders.Poll([folder], Now));
            Assert.IsNull(folders.Find("KosiceO")!.Error);
        }

        CollectionAssert.AreEqual(new[] { "KosiceO" }, folders.Poll([folder], Now).ToArray());
        var source = folders.Find("KosiceO")!;
        Assert.IsNotNull(source.Error);
        Assert.AreEqual("1", source.Table!.Rows[0].Text("number"));

        Write(folder, "KosiceO.xml", Xml(Row("10:00", "3")), time.AddSeconds(60));
        CollectionAssert.AreEqual(new[] { "KosiceO" }, folders.Poll([folder], Now).ToArray());
        source = folders.Find("KosiceO")!;
        Assert.IsNull(source.Error);
        Assert.AreEqual("3", source.Table!.Rows[0].Text("number"));
    }

    [TestMethod]
    public void Poll_ZmiznutySuborAPriecinok()
    {
        var folder = Folder();
        var path = Write(folder, "KosiceO.xml", Xml(""));
        var folders = new XmlTableFolders();
        folders.Poll([folder], Now);
        File.Delete(path);

        CollectionAssert.AreEqual(new[] { "KosiceO" }, folders.Poll([folder], Now).ToArray());
        Assert.IsNull(folders.Find("KosiceO"));
        Assert.IsEmpty(folders.Poll([Path.Combine(folder, "neexistuje")], Now));
    }

    [TestMethod]
    public void Poll_UplynutiePlatnosti_JeZmena()
    {
        var folder = Folder();
        Write(folder, "KosiceO.xml", Xml("", validTo: "2026-09-13 11:46:30"));
        var folders = new XmlTableFolders();
        folders.Poll([folder], Now);

        Assert.IsEmpty(folders.Poll([folder], Now.AddMinutes(5)));
        CollectionAssert.AreEqual(new[] { "KosiceO" }, folders.Poll([folder], Now.AddMinutes(7)).ToArray());
        Assert.IsTrue(folders.Find("KosiceO")!.IsStale(Now.AddMinutes(7)));
    }
}
