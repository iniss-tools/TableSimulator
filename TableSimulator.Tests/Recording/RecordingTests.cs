using System.Text;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Recording;

/// <summary>
/// Zaznam komunikacie a jeho prehratie.
/// </summary>
[TestClass]
public class RecordingTests
{
    private static Simulator Create()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(3, 4, "TCP://127.0.0.1:47003") { Manufacturer = "ELEN10" });
        return sim;
    }

    private static readonly byte[][] Session =
    [
        Hex("01 83 81 80 04 82 84"),
        ElenFrame.Build(3, 1, Hex("82 10 80 1B 52 8E 69 6C 69 6E 61 1B 52")),
        ElenFrame.Build(3, 1, Hex("81 10 80 1B 51 1B 7B 2A 41 7C 42 7D")),
        ElenFrame.Build(7, 1, Hex("81 10 85 1B 62 53 6C 6F 76 61 6B 69 61")),
        Hex("01 83 84 80 E3 04 82 85")
    ];

    /// <summary>Zaznam relacie <see cref="Session" /> - spravy rozdelene na viac kusov ako zo siete.</summary>
    private static async Task<byte[]> RecordAsync(Simulator sim)
    {
        var output = new MemoryStream();
        var recorder = new SessionRecorder(output, sim.ToConfig(), sim.Now, "skuska");
        sim.StartRecording(recorder);
        foreach (var frame in Session)
        {
            sim.Receive(3, frame.AsSpan(0, 3));
            sim.Receive(3, frame.AsSpan(3));
        }

        await sim.StopRecording()!.DisposeAsync();
        return output.ToArray();
    }

    /// <summary>Dohrany zaznam - prehravac potom caka na posun alebo pokracovanie, nekonci.</summary>
    private static async Task FinishedAsync(ReplayPlayer player)
    {
        for (var i = 0; i < 1000 && player.Status.State != ReplayState.Finished; i++)
            await Task.Delay(10);
        Assert.AreEqual(ReplayState.Finished, player.Status.State);
    }

        private static string[] Rows(Simulator sim) => sim.Read(s => s.Boards
        .SelectMany(b => ((ElenDisplay)b.Display).Rows.OrderBy(r => r.Key).Select(r => $"{b.Id}/{r.Key}: {r.Value.PlainText}"))
        .ToArray());

    [TestMethod]
    public void Format_ZapisANacitanie()
    {
        var header = new RecordingHeader(RecordingFile.FormatName, RecordingFile.Version, DateTimeOffset.UnixEpoch, new SimulatorConfig(), "pozn.");
        var file = new RecordingFile(header, [
            new RecordingEntry(0.5, 1, FrameDirection.Received, [0x01, 0x85]),
            new RecordingEntry(1.25, 1, FrameDirection.Sent, [0x06])
        ]);
        using var stream = new MemoryStream();
        file.Write(stream);
        var text = Encoding.UTF8.GetString(stream.ToArray());

        var read = RecordingFile.Read(new MemoryStream(stream.ToArray()));

        StringAssert.Contains(text, "{\"t\":0.500,\"line\":1,\"rx\":\"0185\"}");
        Assert.AreEqual("pozn.", read.Header.Note);
        Assert.HasCount(2, read.Entries);
        Assert.AreEqual(FrameDirection.Sent, read.Entries[1].Direction);
        CollectionAssert.AreEqual(new byte[] { 0x06 }, read.Entries[1].Bytes);
        Assert.AreEqual(1.25, read.Duration);
    }

    [TestMethod]
    public void Format_ZlyRiadokSaPreskoci()
    {
        var text = RecordingFile.HeaderLine(new RecordingHeader(RecordingFile.FormatName, 1, DateTimeOffset.UnixEpoch, new SimulatorConfig(), null))
                   + "\n{\"t\":0,\"line\":1,\"rx\":\"zz\"}\n\n{\"t\":0.1,\"line\":2,\"rx\":\"06\"}\n";

        var read = RecordingFile.Read(new MemoryStream(Encoding.UTF8.GetBytes(text)));

        Assert.AreEqual(2, read.Entries.Single().Line);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("{\"format\":\"iny\",\"version\":1}")]
    [DataRow("nie json")]
    public void Format_ZlaHlavicka(string first) =>
        Assert.ThrowsExactly<FormatException>(() => RecordingFile.Read(new MemoryStream(Encoding.UTF8.GetBytes(first + "\n"))));

    [TestMethod]
    public async Task Zaznam_ObsahujeSpravyAjOdpovede()
    {
        var bytes = await RecordAsync(Create());

        var recording = RecordingFile.Read(new MemoryStream(bytes));

        Assert.AreEqual("skuska", recording.Header.Note);
        Assert.AreEqual(3, recording.Header.Config.Lines.Single().Number);
        Assert.AreEqual(2 * Session.Length, recording.Entries.Count(e => e.Direction == FrameDirection.Received));
        Assert.AreEqual(Session.Length, recording.Entries.Count(e => e.Direction == FrameDirection.Sent));
    }

    [TestMethod]
    public async Task Prehratie_DoNovehoSimulatora_RovnakyObsah()
    {
        var original = Create();
        var recording = RecordingFile.Read(new MemoryStream(await RecordAsync(original)));
        var copy = new Simulator();

        Assert.IsTrue(copy.AddMissing(recording.Header.Config));
        await using var player = new ReplayPlayer(copy, recording, "test", 0, false);
        player.Start();
        await FinishedAsync(player);

        CollectionAssert.AreEqual(Rows(original), Rows(copy));
        Assert.AreEqual(ReplayState.Finished, player.Status.State);
        Assert.AreEqual(99, copy.Read(s => s.FindBoard("3-3"))!.Display.Brightness);
    }

    [TestMethod]
    public async Task Prehratie_NezapisujeSaDoZaznamu()
    {
        var recording = RecordingFile.Read(new MemoryStream(await RecordAsync(Create())));
        var sim = Create();
        var recorder = new SessionRecorder(new MemoryStream(), sim.ToConfig(), sim.Now, null);
        sim.StartRecording(recorder);

        await using var player = new ReplayPlayer(sim, recording, "test", 0, false);
        player.Start();
        await FinishedAsync(player);
        await sim.StopRecording()!.DisposeAsync();

        Assert.AreEqual(0, recorder.Entries);
    }

    [TestMethod]
    public async Task Prehratie_OpakovanieAZastavenie()
    {
        var recording = RecordingFile.Read(new MemoryStream(await RecordAsync(Create())));
        var sim = new Simulator();
        sim.AddMissing(recording.Header.Config);

        await using var player = new ReplayPlayer(sim, recording, "test", 0, true);
        player.Start();
        while (player.Status.Pass < 3)
            await Task.Delay(1);
        await player.StopAsync();

        Assert.AreEqual(ReplayState.Stopped, player.Status.State);
    }

    [TestMethod]
    public async Task Prehratie_PovodnyCasKratRychlost()
    {
        var header = new RecordingHeader(RecordingFile.FormatName, 1, DateTimeOffset.UnixEpoch, Create().ToConfig(), null);
        var recording = new RecordingFile(header, [
            new RecordingEntry(0, 3, FrameDirection.Received, Session[0]),
            new RecordingEntry(0.4, 3, FrameDirection.Received, Session[1])
        ]);
        var sim = Create();

        await using var player = new ReplayPlayer(sim, recording, "test", 2, false);
        var started = DateTime.UtcNow;
        player.Start();
        await FinishedAsync(player);

        // 0,4 s pri dvojnasobnej rychlosti = asi 0,2 s
        Assert.IsGreaterThanOrEqualTo(0.18, (DateTime.UtcNow - started).TotalSeconds);
        Assert.AreEqual("Žilina", ((ElenDisplay)sim.Read(s => s.FindBoard("3-3"))!.Display).Rows[2].PlainText);
    }

    /// <summary>Zaznam s riadkami A, B, C tabule 3 v case 0, 1 a 2 s.</summary>
    private static RecordingFile Abc()
    {
        var header = new RecordingHeader(RecordingFile.FormatName, 1, DateTimeOffset.UnixEpoch, Create().ToConfig(), null);
        return new RecordingFile(header, [
            new RecordingEntry(0, 3, FrameDirection.Received, ElenFrame.Build(3, 1, Hex("81 10 80 1B 52 41"))),
            new RecordingEntry(1, 3, FrameDirection.Received, ElenFrame.Build(3, 1, Hex("81 10 80 1B 52 42"))),
            new RecordingEntry(2, 3, FrameDirection.Received, ElenFrame.Build(3, 1, Hex("81 10 80 1B 52 43")))
        ]);
    }

    private static string Text(Simulator sim) =>
        sim.Read(s => s.FindBoard("3-3") is { Display: ElenDisplay display } && display.Rows.TryGetValue(1, out var row) ? row.PlainText : "");

    [TestMethod]
    public async Task Posun_ObsahZMiestaBezZapisuDoKomunikacieAPocitadiel()
    {
        var sim = Create();
        sim.Receive(3, ElenFrame.Build(3, 1, Hex("81 10 80 1B 52 58")));
        var frames = sim.Read(s => s.FindBoard("3-3")!.Stats.Frames);
        var logged = sim.LogSince(0).Count;
        await using var player = new ReplayPlayer(sim, Abc(), "abc", 1, false);
        player.Start(paused: true);

        await player.SeekAsync(1.5);
        Assert.AreEqual("B", Text(sim));
        Assert.AreEqual(ReplayState.Paused, player.Status.State);
        Assert.AreEqual(1.5, player.Status.Position);

        // dozadu: tabula sa zmaze a prehra od zaciatku - predchadzajuci obsah X zmizne
        await player.SeekAsync(0.5);
        Assert.AreEqual("A", Text(sim));
        Assert.AreEqual(frames, sim.Read(s => s.FindBoard("3-3")!.Stats.Frames));
        Assert.HasCount(logged, sim.LogSince(0));

        // pokracovanie bez cakania dohra zvysok
        player.SetSpeed(0);
        player.Resume();
        await FinishedAsync(player);
        Assert.AreEqual("C", Text(sim));
        Assert.AreEqual(frames + 2, sim.Read(s => s.FindBoard("3-3")!.Stats.Frames));
    }

    [TestMethod]
    public async Task Pauza_ZastaviCasAPokracovanie()
    {
        var sim = Create();
        var header = new RecordingHeader(RecordingFile.FormatName, 1, DateTimeOffset.UnixEpoch, Create().ToConfig(), null);
        var recording = new RecordingFile(header, [.. Abc().Entries.Select(e => e with { Time = e.Time * 0.3 })]);
        await using var player = new ReplayPlayer(sim, recording, "abc", 1, false);

        player.Start();
        for (var i = 0; i < 100 && Text(sim) != "A"; i++)
            await Task.Delay(5);
        player.Pause();
        var paused = player.Status.Position;
        await Task.Delay(500);

        Assert.AreEqual("A", Text(sim));
        Assert.AreEqual(paused, player.Status.Position);
        player.Resume();
        await FinishedAsync(player);
        Assert.AreEqual("C", Text(sim));
    }

    [TestMethod]
    public async Task Rychlost_ZmenaPocasPrehravania()
    {
        var sim = Create();
        await using var player = new ReplayPlayer(sim, Abc(), "abc", 1, false);
        var started = DateTime.UtcNow;

        player.Start();
        await Task.Delay(100);
        player.SetSpeed(10);
        await FinishedAsync(player);

        // 2 s zaznamu: 0,1 s povodnou rychlostou a zvysok desatnasobnou - daleko pod 2 s
        Assert.IsLessThan(1.2, (DateTime.UtcNow - started).TotalSeconds);
        Assert.AreEqual(10, player.Status.Speed);
        Assert.AreEqual(2, player.Status.Position);
    }

    [TestMethod]
    public async Task Dohrane_PosunPozastaviNaMiesteAPokracovanieZacneOdZaciatku()
    {
        var sim = Create();
        await using var player = new ReplayPlayer(sim, Abc(), "abc", 0, false);
        player.Start();
        await FinishedAsync(player);

        await player.SeekAsync(1.5);
        Assert.AreEqual((ReplayState.Paused, "B"), (player.Status.State, Text(sim)));

        player.Resume();
        await FinishedAsync(player);
        Assert.AreEqual("C", Text(sim));

        player.Resume();
        await FinishedAsync(player);
        Assert.AreEqual(2, player.Status.Pass);
    }
}
