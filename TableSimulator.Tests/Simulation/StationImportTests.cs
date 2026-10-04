using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Simulation;

/// <summary>
/// Import liniek a tabul stanice (z GVDEditora pri presmerovani) a zmena triedy linky.
/// </summary>
[TestClass]
public class StationImportTests
{
    private static StationImport Station() => new()
    {
        Lines =
        [
            new StationLine(3, 4, 47003) { Name = "Driver" },
            new StationLine(4, 5, 47004) { Name = "Driver0" },
            new StationLine(5, 10, 47005) { Name = "Driver1" }
        ],
        Boards =
        [
            new StationBoard(3, 5, "ELEN16") { Name = "ODCH1" },
            new StationBoard(4, 12, "LCD1") { Name = "NAST1" },
            new StationBoard(4, 13, "FERS") { Name = "PT" },
            new StationBoard(5, 130, "APEL") { Name = "APEL" }
        ]
    };

    [TestMethod]
    public void Import_ZalozLinkyATabule()
    {
        var sim = new Simulator();

        var result = sim.ImportStation(Station());

        Assert.AreEqual(2, result.LinesAdded);
        Assert.AreEqual(2, result.BoardsAdded);
        CollectionAssert.AreEqual(new[] { 5 }, result.UnsupportedLines.ToArray(), "APEL simulator nepozna");
        CollectionAssert.AreEqual(new[] { "4-13" }, result.SkippedBoards.ToArray(), "FERS na linke LCD1");
        var line = sim.Read(s => s.FindLine(3))!;
        Assert.AreEqual("TCP://0.0.0.0:47003", line.Endpoint.ToString());
        Assert.AreEqual("ELEN16", line.Manufacturer, "samoucenie podla tabul linky");
        var board = sim.Read(s => s.FindBoard("4-12"))!;
        Assert.AreEqual("NAST1", board.Name);
        Assert.IsTrue(board.Profile.AutoSize);
        Assert.IsInstanceOfType<CharDisplay>(board.Display);
    }

    [TestMethod]
    public void OpakovanyImport_NechaNastavenieObsluhy()
    {
        var sim = new Simulator();
        sim.ImportStation(Station());
        sim.SetBoard(new BoardConfig(3, 5, "ELEN16") { Name = "Upravena", Profile = new BoardProfile { Width = 160 } });

        var result = sim.ImportStation(Station() with { Lines = [new StationLine(3, 4, 48003)] });

        Assert.AreEqual(0, result.LinesAdded);
        Assert.AreEqual(1, result.LinesUpdated);
        Assert.AreEqual(2, result.BoardsKept);
        var board = sim.Read(s => s.FindBoard("3-5"))!;
        Assert.AreEqual("Upravena", board.Name);
        Assert.AreEqual(160, board.Profile.Width);
        Assert.AreEqual(48003, sim.Read(s => s.FindLine(3))!.Endpoint.Port);
    }

    [TestMethod]
    public void ZmenaTriedyLinky_TabuleDostanuObsahNovejRodiny()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001") { AutoLearn = false, Manufacturer = "ELEN10" });
        sim.SetBoard(new BoardConfig(1, 12, "ELEN10"));

        sim.ImportStation(new StationImport { Lines = [new StationLine(1, 5, 47001)], Boards = [new StationBoard(1, 12, "ERP")] });
        var reply = sim.Receive(1, Hex("0C 05 00 40 51")).SelectMany(r => r.Bytes).ToArray();

        Assert.IsInstanceOfType<CharDisplay>(sim.Read(s => s.FindBoard("1-12"))!.Display);
        CollectionAssert.AreEqual(Hex("00 06 0C 70 00 82"), reply);
        var line = sim.Read(s => s.FindLine(1))!;
        Assert.AreEqual("ERP", line.Manufacturer, "vyrobca podla tabul stanice");
        Assert.AreEqual("127.0.0.1", line.Endpoint.Host, "adresa existujucej linky ostane");
    }

    [TestMethod]
    public void LinkaMimoRozsahu_Vynecha()
    {
        var sim = new Simulator();

        var result = sim.ImportStation(new StationImport { Lines = [new StationLine(0, 4, 47000)] });

        CollectionAssert.AreEqual(new[] { 0 }, result.UnsupportedLines.ToArray());
        Assert.AreEqual(0, sim.Read(s => s.Lines.Count()));
    }
}
