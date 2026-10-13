using ModelContextProtocol;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Frames;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Recordings;
using TableSimulator.Contracts.Settings;
using TableSimulator.Contracts.State;
using TableSimulator.Contracts.Statistics;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;
using TableSimulator.Mcp;
using FaultKind = TableSimulator.Contracts.Boards.FaultKind;

namespace TableSimulator.Tests.Mcp;

/// <summary>Strucne vysledky nastrojov MCP a chyby pre model.</summary>
[TestClass]
public sealed class McpMapperTests
{
    /// <summary>Linka 1 s tabulami 1-1 az 1-3, linka 2 s tabulou 2-1.</summary>
    private static StateDto State(Func<BoardDto, BoardDto> change)
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001") { AutoLearn = false });
        sim.SetLine(new LineConfig(2, 4, "TCP://127.0.0.1:47002") { AutoLearn = false });
        foreach (var (line, address) in new[] { (1, 1), (1, 2), (1, 3), (2, 1) })
            sim.SetBoard(new BoardConfig(line, address, "ELEN") { Profile = BoardProfile.DefaultFor("ELEN") });
        return sim.Read(s => new StateDto(
            s.Lines.Select(l => DtoMapper.ToDto(l, null)).ToArray(),
            s.Boards.Select(DtoMapper.ToDto).Select(change).ToArray(),
            [], new Dictionary<string, string[]>(), new SettingsDto(5470, false, "", "", "", "")));
    }

    private static FrameDto Frame(long seq, int line, int? address, FrameStatus status = FrameStatus.Ok) =>
        new(seq, DateTimeOffset.UnixEpoch.AddSeconds(seq), line, FrameDirection.Received, address, "", status, "text", null);

    [TestMethod]
    public void Status_PocetTabulALenTabuleSProblemom()
    {
        var state = State(b => b.Id switch
        {
            "1-2" => b with { Fault = new BoardFaultDto(FaultKind.NoReply) },
            "1-3" => b with { PoweredOff = true },
            "2-1" => b with { Mismatch = new ManufacturerMismatchDto("elen.extendedFont", ["ELEN16"]) },
            _ => b
        });

        var status = McpMapper.ToStatus(state);

        CollectionAssert.AreEqual(new[] { 3, 1 }, status.Lines.Select(l => l.Boards).ToArray());
        CollectionAssert.AreEqual(new[] { "1-2", "1-3", "2-1" }, status.Issues.Select(i => i.Id).ToArray());
        Assert.AreEqual(FaultKind.NoReply, status.Issues[0].Fault);
        Assert.IsTrue(status.Issues[1].PoweredOff);
        Assert.AreEqual("elen.extendedFont", status.Issues[2].Mismatch?.Evidence);
    }

    [TestMethod]
    public void Status_BezProblemov_PrazdnyZoznam()
    {
        Assert.IsEmpty(McpMapper.ToStatus(State(b => b)).Issues);
    }

    [TestMethod]
    public void Frames_FilterLinkyAdresyAProblemov()
    {
        FrameDto[] log = [Frame(1, 1, 5), Frame(2, 1, 6), Frame(3, 2, 5), Frame(4, 1, 5, FrameStatus.BadChecksum), Frame(5, 1, null, FrameStatus.Noise)];

        CollectionAssert.AreEqual(new long[] { 1, 4 }, McpMapper.ToFrames(log, 0, 1, 5, false, 50).Frames.Select(f => f.Seq).ToArray());
        CollectionAssert.AreEqual(new long[] { 4, 5 }, McpMapper.ToFrames(log, 0, null, null, true, 50).Frames.Select(f => f.Seq).ToArray());
        CollectionAssert.AreEqual(new long[] { 4, 5 }, McpMapper.ToFrames(log, 3, 1, null, false, 50).Frames.Select(f => f.Seq).ToArray());
    }

    [TestMethod]
    public void Frames_LimitNajnovsieAPokracovanie()
    {
        FrameDto[] log = [Frame(1, 1, 5), Frame(2, 1, 5), Frame(3, 1, 5), Frame(4, 2, 5)];

        var limited = McpMapper.ToFrames(log, 0, 1, null, false, 2);
        var all = McpMapper.ToFrames(log, 0, 1, null, false, 3);

        CollectionAssert.AreEqual(new long[] { 2, 3 }, limited.Frames.Select(f => f.Seq).ToArray());
        Assert.IsTrue(limited.Truncated);
        Assert.IsFalse(all.Truncated);
        // pokracovanie od najnovsej spravy logu, aj ked filter ju vynechal
        Assert.AreEqual(4, limited.Next);
        Assert.AreEqual(7, McpMapper.ToFrames([], 7, null, null, false, 50).Next);
    }

    [TestMethod]
    public void Chyba_KodAPopisPreModel()
    {
        var validation = new ValidationError([Error.Problem("log.after", "After must not be negative."), Error.Problem("log.limit", "Too many.")]);

        Assert.AreEqual("board.notFound: Board 1-9 does not exist.", McpResults.Describe(Error.NotFound("board.notFound", "Board 1-9 does not exist.")));
        Assert.AreEqual("log.after: After must not be negative.; log.limit: Too many.", McpResults.Describe(validation));
        // neocakavana chyba neprezradi podrobnosti - ako ProblemDetails API
        Assert.AreEqual("db.failed: An unexpected error occurred.", McpResults.Describe(Error.Failure("db.failed", @"C:\data\tablesimulator.db is locked")));
    }

    [TestMethod]
    public void Vysledok_ChybaAkoVynimkaNastroja()
    {
        var error = Assert.ThrowsExactly<McpException>(() => Result.Failure<int>(Error.NotFound("board.notFound", "Board 1-9 does not exist.")).ValueOrThrow());

        Assert.AreEqual("board.notFound: Board 1-9 does not exist.", error.Message);
        Assert.AreEqual(5, Result.Success(5).ValueOrThrow());
    }

    [TestMethod]
    public void Tabule_VerziaZTextuARiadkyLenNaZiadost()
    {
        var state = State(b => b);
        BoardTextDto[] texts = [new("1-1", 42, false, ["Praha", ""])];

        var withText = McpMapper.ToBoards(state.Boards, texts);
        var without = McpMapper.ToBoards(state.Boards, null);

        // verzia patri k riadkom, ktore model vidi
        Assert.AreEqual(42, withText.Boards[0].Version);
        CollectionAssert.AreEqual(new[] { "Praha", "" }, withText.Boards[0].Rows!.ToArray());
        // tabula zalozena medzi dvoma citaniami - bez riadkov
        Assert.IsNull(withText.Boards[1].Rows);
        Assert.IsTrue(without.Boards.All(b => b.Rows is null));
        Assert.AreEqual(state.Boards[0].Version, without.Boards[0].Version);
    }

    [TestMethod]
    public void Detail_SurovyObsahLenNaZiadost()
    {
        var board = State(b => b).Boards[0];

        var plain = McpMapper.ToBoardDetail(board, new BoardTextDto(board.Id, 7, false, ["A"]), false, []);
        var raw = McpMapper.ToBoardDetail(board, null, true, []);

        Assert.IsNull(plain.Profile);
        Assert.IsNull(plain.Display);
        Assert.AreEqual(7, plain.Version);
        Assert.IsNotNull(raw.Profile);
        Assert.IsNotNull(raw.Display);
        Assert.IsEmpty(raw.Rows);
    }

    private static BoardStateDto S(double time, string row) => new(time, time, [row]);

    private static ComparisonStepDto Step(ComparisonStepKind kind) => new(kind, kind == ComparisonStepKind.Added ? null : S(0, "a"), kind == ComparisonStepKind.Removed ? null : S(0, "b"));

    [TestMethod]
    public void Porovnanie_PoctyZoVsetkychFilterAKroky()
    {
        var side = new RecordingSideDto("a", DateTimeOffset.UnixEpoch, 10, null);
        var comparison = new RecordingComparisonDto(side, side with { Name = "b" },
        [
            new BoardComparisonDto("1-1", "", BoardDifference.Same, 1, 1, [Step(ComparisonStepKind.Same)]),
            new BoardComparisonDto("1-2", "", BoardDifference.Different, 3, 3,
                [Step(ComparisonStepKind.Same), Step(ComparisonStepKind.Changed), Step(ComparisonStepKind.Same), Step(ComparisonStepKind.Changed), Step(ComparisonStepKind.Added)]),
            new BoardComparisonDto("1-3", "", BoardDifference.OnlyA, 1, 0, [Step(ComparisonStepKind.Removed)])
        ]);

        var differences = McpMapper.ToComparison(comparison, null, true, 50);
        var all = McpMapper.ToComparison(comparison, null, false, 50);
        var one = McpMapper.ToComparison(comparison, "1-2", true, 2);

        Assert.AreEqual((1, 1, 1, 0), (differences.Same, differences.Different, differences.OnlyA, differences.OnlyB));
        CollectionAssert.AreEqual(new[] { "1-2", "1-3" }, differences.Boards.Select(b => b.Id).ToArray());
        Assert.HasCount(3, differences.Boards[0].Steps);
        Assert.HasCount(3, all.Boards);
        Assert.HasCount(5, all.Boards[1].Steps);
        // pocty su vzdy zo vsetkych tabul, zoznam len vybranej
        Assert.AreEqual(1, one.Same);
        CollectionAssert.AreEqual(new[] { "1-2" }, one.Boards.Select(b => b.Id).ToArray());
        Assert.HasCount(2, one.Boards[0].Steps);
        CollectionAssert.AreEqual(new[] { "1-2" }, one.TruncatedBoards.ToArray());
        Assert.IsEmpty(differences.TruncatedBoards);
    }

    [TestMethod]
    public void ZaznamTabule_StavVCaseALimit()
    {
        var timeline = new BoardTimelineDto("3-3", "Nastupiste", 20, [S(2, "A"), S(10, "B"), S(15, "C")]);

        Assert.AreEqual("B", McpMapper.ToRecordingBoard(timeline, 12, 100).States.Single().Rows[0]);
        Assert.AreEqual("B", McpMapper.ToRecordingBoard(timeline, 10, 100).States.Single().Rows[0]);
        // pred prvou zmenou tabula este nic neukazovala
        Assert.IsEmpty(McpMapper.ToRecordingBoard(timeline, 1, 100).States);
        var limited = McpMapper.ToRecordingBoard(timeline, null, 2);
        CollectionAssert.AreEqual(new[] { "A", "B" }, limited.States.Select(s => s.Rows[0]).ToArray());
        Assert.IsTrue(limited.Truncated);
        Assert.IsFalse(McpMapper.ToRecordingBoard(timeline, null, 3).Truncated);
    }

    [TestMethod]
    public void Statistika_SuctyANajdlhsiaPauza()
    {
        var t = DateTimeOffset.UnixEpoch;
        LineTrafficPointDto Line(int line, int ok) => new(line, t, ok, 1, 0, 0, 0, ok, 0, 100, 50, 1, 0);
        BoardTrafficPointDto Board(int address, int frames, long? gap) => new(1, address, t, frames, frames, 0, 0, gap);
        var statistics = new TrafficStatisticsDto(t, t.AddHours(1), 1, [Line(2, 4), Line(1, 3), Line(1, 5)], [Board(5, 2, null), Board(5, 3, 300), Board(6, 1, null)],
            [], 30);

        var totals = McpMapper.ToStatistics(statistics, false);
        var series = McpMapper.ToStatistics(statistics, true);

        CollectionAssert.AreEqual(new[] { 1, 2 }, totals.Lines.Select(l => l.Line).ToArray());
        Assert.AreEqual((8, 2, 200L, 2), (totals.Lines[0].Ok, totals.Lines[0].BadChecksum, totals.Lines[0].BytesIn, totals.Lines[0].Connections));
        CollectionAssert.AreEqual(new[] { "1-5", "1-6" }, totals.Boards.Select(b => b.Id).ToArray());
        Assert.AreEqual((5, 300L), (totals.Boards[0].Frames, totals.Boards[0].MaxGapMs));
        Assert.IsNull(totals.Boards[1].MaxGapMs);
        Assert.IsNull(totals.Series);
        Assert.HasCount(3, series.Series!);
    }

    [TestMethod]
    public void ZmenaLinky_LenZadaneHodnotyNovaPovinne()
    {
        var current = State(b => b).Lines[0];

        var renamed = McpRequests.Line(current, 1, null, null, "Hala", null, null, null);
        var created = McpRequests.Line(null, 9, 10, "TCP://0.0.0.0:47009", null, null, null, null);

        Assert.AreEqual(("Hala", current.TableClass, current.Endpoint, current.AutoLearn, current.Manufacturer),
            (renamed.Name, renamed.TableClass, renamed.Endpoint, renamed.AutoLearn, renamed.Manufacturer));
        // nova linka: prvy vyrobca triedy, samoucenie zapnute
        Assert.AreEqual(("APEL", true, LineMode.Display), (created.Manufacturer, created.AutoLearn, created.Mode));
        StringAssert.StartsWith(Assert.ThrowsExactly<McpException>(() => McpRequests.Line(null, 9, null, "TCP://0.0.0.0:47009", null, null, null, null)).Message,
            "line.new");
    }

    [TestMethod]
    public void ZmenaTabule_ProfilPodlaVyrobcuADruhaAdresa()
    {
        var current = State(b => b).Boards[0] with { SecondAddress = 7 };

        var renamed = McpRequests.Board(current, 1, 1, null, "Hala", null, 4, null, null);
        var single = McpRequests.Board(current, 1, 1, null, null, null, null, null, 0);
        var created = McpRequests.Board(null, 5, 0x81, "APEL", "Vestibul", null, null, null, null);

        Assert.AreEqual(("Hala", current.Manufacturer, 4, current.Profile.Width, 7),
            (renamed.Name, renamed.Manufacturer, renamed.Profile.Rows, renamed.Profile.Width, renamed.SecondAddress));
        Assert.IsNull(single.SecondAddress);
        Assert.AreEqual(DtoMapper.ToDto(BoardProfile.DefaultFor("APEL")).Rows, created.Profile.Rows);
        Assert.AreEqual(FaultKind.None, created.Fault.Kind);
        StringAssert.StartsWith(Assert.ThrowsExactly<McpException>(() => McpRequests.Board(null, 5, 1, null, null, null, null, null, null)).Message, "board.new");
    }

    [TestMethod]
    public void Vzhlad_PouzityNastavenyAleboPredvolenyVyrobcu()
    {
        var board = State(b => b).Boards[0];

        Assert.AreEqual("elen-led", McpMapper.DesignInUse(board, ["elen-led", "moj"]));
        Assert.AreEqual("moj", McpMapper.DesignInUse(board with { Design = "moj" }, ["elen-led", "moj"]));
        // zmazany vzhlad prehliadac nahradi predvolenym
        Assert.AreEqual("elen-led", McpMapper.DesignInUse(board with { Design = "zmazany" }, ["elen-led"]));
    }

    /// <summary>Rovnake pripady ako test defaultDesignId vo web/src/render/design.test.ts.</summary>
    [TestMethod]
    [DataRow("flaps", "ERS", "listova")]
    [DataRow("chars", "LCD1", "lcd-dark")]
    [DataRow("chars", "apel_an", "apel-led")]
    [DataRow("elen", "APEL", "apel-led")]
    [DataRow("elen", "ELEN16", "elen-led")]
    [DataRow("amplifier", "ELSVO", "elen-led")]
    public void Vzhlad_PredvolenyAkoNaWebe(string family, string manufacturer, string design) =>
        Assert.AreEqual(design, TableSimulator.Application.Designs.DesignDefaults.For(family, manufacturer));

    [TestMethod]
    public void Profil_CiastocnaZmenaNullVratiPredvolene()
    {
        var current = new BoardProfileDto { Rows = 3, Face = "hala", FaceValues = new Dictionary<string, string> { ["track"] = "1", ["side"] = "A" } };
        using var patch = System.Text.Json.JsonDocument.Parse("""{"rows":4,"faceValues":{"track":"2","side":null},"face":null}""");

        var changed = McpRequests.PatchProfile(current, patch.RootElement);

        Assert.AreEqual(4, changed.Rows);
        Assert.IsNull(changed.Face);
        CollectionAssert.AreEquivalent(new[] { "track" }, changed.FaceValues!.Keys.ToArray());
        Assert.AreEqual("2", changed.FaceValues["track"]);
        // ostatne bez zmeny
        Assert.AreEqual(current.Width, changed.Width);
        using var array = System.Text.Json.JsonDocument.Parse("[]");
        StringAssert.StartsWith(Assert.ThrowsExactly<McpException>(() => McpRequests.PatchProfile(current, array.RootElement)).Message, "board.profile");
    }

    [TestMethod]
    public void DniArchivu_OdNajnovsiehoNajviac31()
    {
        var days = Enumerable.Range(0, 40).Select(i => new DateOnly(2026, 9, 1).AddDays(i)).ToList();

        var recent = McpMapper.RecentDays(days);

        Assert.HasCount(McpMapper.ArchiveDays, recent);
        Assert.AreEqual(new DateOnly(2026, 10, 10), recent[0]);
        Assert.AreEqual(new DateOnly(2026, 9, 10), recent[^1]);
    }
}
