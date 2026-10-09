using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Tests.Recording;

/// <summary>
/// Porovnanie zaznamov: priebeh textu tabul z prehratia bez cakania, stav tabule v case a porovnanie postupnosti
/// stavov (na case nezalezi, len na poradi).
/// </summary>
[TestClass]
public class RecordingComparisonTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 9, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Riadok textu tabuli ELEN (prikaz 1, pismo 0x52).</summary>
    private static byte[] Row(int address, int row, string text) =>
        ElenFrame.Build(address, 1, [(byte)(0x80 | row), 0x10, 0x80, 0x1B, 0x52, .. Encodings.Win1250.GetBytes(text)]);

    private static RecordingFile Recording(params (double Time, byte[] Bytes)[] frames)
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(3, 4, "TCP://127.0.0.1:47003") { Manufacturer = "ELEN10" });
        var header = new RecordingHeader(RecordingFile.FormatName, RecordingFile.Version, Start, sim.ToConfig(), null);
        return new RecordingFile(header, [.. frames.Select(f => new RecordingEntry(f.Time, 3, FrameDirection.Received, f.Bytes))]);
    }

    private static BoardState State(double time, params string[] rows) => new(time, rows);

    [TestMethod]
    public void Priebeh_KazdaZmenaTextuTabule()
    {
        var recording = Recording((0, Row(3, 2, "Žilina")), (5, Row(3, 1, "Os 1234")), (6, Row(3, 1, "Os 1234")), (10, Row(3, 2, "Košice")),
            (11, Row(7, 1, "Bratislava")));

        var timelines = RecordingTimeline.Build(recording);

        CollectionAssert.AreEqual(new[] { "3-3", "3-7" }, timelines.Select(t => t.Id).ToArray());
        var states = timelines[0].States;
        // rovnaky riadok znova (6 s) nie je zmena
        CollectionAssert.AreEqual(new[] { 0d, 5d, 10d }, states.Select(s => s.Time).ToArray());
        Assert.AreEqual("Žilina", states[0].Rows[^2]);
        Assert.AreEqual("Os 1234", states[1].Rows[^1]);
        Assert.AreEqual("Košice", states[2].Rows[^2]);
        Assert.AreEqual("Bratislava", timelines[1].States.Single().Rows[^1]);
    }

    [TestMethod]
    public void Priebeh_RiadkyTesneZaSebouJedenStav()
    {
        // INISS posiela riadky po jednom - polovicne prepisana tabula nie je stav
        var recording = Recording((10, Row(3, 2, "Žilina")), (10.05, Row(3, 1, "Os 1234")), (10.1, Row(3, 2, "Košice")), (30, Row(3, 1, "Os 1236")));

        var states = RecordingTimeline.Build(recording).Single().States;

        CollectionAssert.AreEqual(new[] { 10d, 30d }, states.Select(s => s.Time).ToArray());
        Assert.AreEqual(10.1, states[0].Settled, 1e-9);
        Assert.AreEqual("Košice", states[0].Rows[^2]);
        Assert.AreEqual("Os 1234", states[0].Rows[^1]);
        Assert.AreEqual("Os 1236", states[1].Rows[^1]);
    }

    [TestMethod]
    public void StavVCase_LenPrevadzkaDovtedy()
    {
        var recording = Recording((0, Row(3, 2, "Žilina")), (5, Row(3, 1, "Os 1234")), (10, Row(3, 2, "Košice")));

        var board = RecordingTimeline.ReplayUntil(recording, 5).Read(s => s.FindBoard("3-3"))!;

        var rows = ((ElenDisplay)board.Display).Rows;
        Assert.AreEqual("Žilina", rows[2].PlainText);
        Assert.AreEqual("Os 1234", rows[1].PlainText);
    }

    [TestMethod]
    public void Porovnanie_ZmenenyAPridanyStav()
    {
        var steps = TimelineDiff.Compare(
            [State(0, "A"), State(1, "B"), State(2, "C")],
            [State(30, "A"), State(31, "X"), State(32, "C"), State(33, "D")]);

        CollectionAssert.AreEqual(new[] { DiffKind.Same, DiffKind.Changed, DiffKind.Same, DiffKind.Added }, steps.Select(s => s.Kind).ToArray());
        Assert.AreEqual("B", steps[1].A!.Rows[0]);
        Assert.AreEqual("X", steps[1].B!.Rows[0]);
        // cas sa neporovnava - kazdy stav si nesie svoj
        Assert.AreEqual(30d, steps[0].B!.Time);
    }

    [TestMethod]
    public void Porovnanie_OdobranyStavUprostred()
    {
        var steps = TimelineDiff.Compare(
            [State(0, "A"), State(1, "B"), State(2, "C"), State(3, "D")],
            [State(0, "A"), State(2, "C"), State(3, "D")]);

        CollectionAssert.AreEqual(new[] { DiffKind.Same, DiffKind.Removed, DiffKind.Same, DiffKind.Same }, steps.Select(s => s.Kind).ToArray());
        Assert.AreEqual("B", steps[1].A!.Rows[0]);
    }

    [TestMethod]
    public void Porovnanie_PresunutyStavAPrazdnePriebehy()
    {
        var steps = TimelineDiff.Compare([State(0, "A"), State(1, "B"), State(2, "C")], [State(0, "B"), State(1, "C"), State(2, "A")]);
        Assert.AreEqual(2, steps.Count(s => s.Kind == DiffKind.Same));
        Assert.AreEqual(1, steps.Count(s => s.Kind == DiffKind.Removed));
        Assert.AreEqual(1, steps.Count(s => s.Kind == DiffKind.Added));

        Assert.IsEmpty(TimelineDiff.Compare([], []));
        CollectionAssert.AreEqual(new[] { DiffKind.Added, DiffKind.Added }, TimelineDiff.Compare([], [State(0, "A"), State(1, "B")]).Select(s => s.Kind).ToArray());
    }
}
