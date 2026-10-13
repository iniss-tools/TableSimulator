using System.Reflection;
using System.Text;
using TableSimulator.Application.Boards;
using TableSimulator.Contracts.Boards;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Apel;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Tests.Application;

/// <summary>Text zivych tabul a cakanie na zmenu tabule (nastroje MCP).</summary>
[TestClass]
public sealed class BoardTextQueriesTests
{
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(10);

    public TestContext TestContext { get; set; } = null!;

    /// <summary>Linka 10 s tabulami 10-129 a 10-130, linka 11 s tabulou 11-129 (APEL).</summary>
    private static Simulator Create()
    {
        var sim = new Simulator();
        foreach (var line in new[] { 10, 11 })
            sim.SetLine(new LineConfig(line, 10, $"TCP://127.0.0.1:470{line}") { AutoLearn = false, Manufacturer = "APEL" });
        foreach (var (line, address) in new[] { (10, 0x81), (10, 0x82), (11, 0x81) })
            sim.SetBoard(new BoardConfig(line, address, "APEL") { Profile = BoardProfile.DefaultFor("APEL") });
        return sim;
    }

    /// <summary>Sprava od INISSu: prvy riadok tabule.</summary>
    private static void Write(Simulator sim, int line, int address, string text) =>
        sim.Receive(line, ApelFrame.Build(address, new ApelRecord(0, Encoding.ASCII.GetBytes(text))));

    private static long VersionOf(Simulator sim, string id) => sim.Read(s => s.FindBoard(id)!.Version);

    private Task<TableSimulator.Application.Abstractions.Results.Result<BoardChangeDto>> Wait(Simulator sim, string id, long after, TimeSpan timeout,
        CancellationToken? cancellationToken = null) =>
        new WaitForBoardChangeQueryHandler(sim, TimeProvider.System).Handle(new WaitForBoardChangeQuery(id, after, timeout),
            cancellationToken ?? TestContext.CancellationToken);

    [TestMethod]
    public async Task Texty_RiadkyZhoraAFilterLinky()
    {
        var sim = Create();
        Write(sim, 10, 0x81, "Praha");
        var handler = new GetBoardTextsQueryHandler(sim);

        var all = (await handler.Handle(new GetBoardTextsQuery(null), TestContext.CancellationToken)).Value;
        var line11 = (await handler.Handle(new GetBoardTextsQuery(11), TestContext.CancellationToken)).Value;

        CollectionAssert.AreEqual(new[] { "10-129", "10-130", "11-129" }, all.Select(b => b.Id).ToArray());
        Assert.AreEqual("Praha", all[0].Rows[0]);
        Assert.IsTrue(all[1].Rows.All(r => r.Length == 0));
        CollectionAssert.AreEqual(new[] { "11-129" }, line11.Select(b => b.Id).ToArray());
    }

    [TestMethod]
    public async Task Texty_ZhasnutaTabulaBezRiadkov()
    {
        var sim = Create();
        Write(sim, 10, 0x81, "Praha");
        sim.SetPower("10-129", false);

        var board = (await new GetBoardTextsQueryHandler(sim).Handle(new GetBoardTextsQuery(10), TestContext.CancellationToken)).Value[0];

        Assert.IsTrue(board.PoweredOff);
        Assert.IsEmpty(board.Rows);
    }

    [TestMethod]
    public void Texty_LinkaMimoRozsahu()
    {
        var validator = new GetBoardTextsQueryValidator();

        Assert.AreEqual(BoardErrors.Line, validator.Validate(new GetBoardTextsQuery(0)).Errors.Single().ErrorCode);
        Assert.IsTrue(validator.Validate(new GetBoardTextsQuery(null)).IsValid);
        Assert.IsTrue(validator.Validate(new GetBoardTextsQuery(100)).IsValid);
    }

    [TestMethod]
    public async Task Cakanie_PoZmeneVratiNovyObsah()
    {
        var sim = Create();
        var before = VersionOf(sim, "10-129");

        var waiting = Wait(sim, "10-129", before, Long);
        Assert.IsFalse(waiting.IsCompleted);
        Write(sim, 10, 0x82, "Ina tabula");
        Write(sim, 10, 0x81, "Brno");
        var result = (await waiting).Value;

        Assert.IsTrue(result.Changed);
        Assert.AreEqual("Brno", result.Board.Rows[0]);
        Assert.IsGreaterThan(before, result.Board.Version);
    }

    [TestMethod]
    public async Task Cakanie_UzZmenenaVratiHned()
    {
        var sim = Create();
        var before = VersionOf(sim, "10-129");
        Write(sim, 10, 0x81, "Praha");

        var waiting = Wait(sim, "10-129", before, Long);

        Assert.IsTrue(waiting.IsCompleted);
        Assert.IsTrue((await waiting).Value.Changed);
    }

    [TestMethod]
    public async Task Cakanie_PoLimiteBezZmeny()
    {
        var sim = Create();
        Write(sim, 10, 0x81, "Praha");
        var version = VersionOf(sim, "10-129");

        var result = (await Wait(sim, "10-129", version, TimeSpan.FromMilliseconds(100))).Value;

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(version, result.Board.Version);
        Assert.AreEqual("Praha", result.Board.Rows[0]);
    }

    [TestMethod]
    public async Task Cakanie_NeznamaAZmazanaTabula()
    {
        var sim = Create();

        var unknown = await Wait(sim, "10-1", 0, Long);
        var waiting = Wait(sim, "10-129", VersionOf(sim, "10-129"), Long);
        sim.RemoveBoard("10-129");
        var removed = await waiting;

        Assert.AreEqual("board.notFound", unknown.Error.Code);
        Assert.AreEqual("board.notFound", removed.Error.Code);
    }

    [TestMethod]
    public async Task Cakanie_ZrusenieKlientomAOdhlasenieUdalosti()
    {
        var sim = Create();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);

        var waiting = Wait(sim, "10-129", VersionOf(sim, "10-129"), Long, cancel.Token);
        Assert.IsNotNull(ChangedHandlers(sim));
        await cancel.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => waiting);
        Assert.IsNull(ChangedHandlers(sim), "cakanie sa z udalosti simulatora neodhlasilo");
    }

    /// <summary>Odberatelia udalosti <see cref="Simulator.Changed" /> - po cakani nesmie ostat ziadny.</summary>
    private static Delegate? ChangedHandlers(Simulator sim) =>
        (Delegate?)typeof(Simulator).GetField(nameof(Simulator.Changed), BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sim);

    [TestMethod]
    public void Cakanie_NeplatneVstupy()
    {
        var validator = new WaitForBoardChangeQueryValidator();

        string[] Codes(WaitForBoardChangeQuery q) => validator.Validate(q).Errors.Select(e => e.ErrorCode).ToArray();

        CollectionAssert.AreEqual(new[] { WaitForBoardChangeQueryValidator.Timeout }, Codes(new("10-129", 0, TimeSpan.Zero)));
        CollectionAssert.AreEqual(new[] { WaitForBoardChangeQueryValidator.Timeout }, Codes(new("10-129", 0, TimeSpan.FromSeconds(61))));
        CollectionAssert.AreEqual(new[] { WaitForBoardChangeQueryValidator.Version }, Codes(new("10-129", -1, TimeSpan.FromSeconds(1))));
        Assert.IsEmpty(Codes(new("10-129", 0, WaitForBoardChangeQueryValidator.MaxTimeout)));
    }
}
