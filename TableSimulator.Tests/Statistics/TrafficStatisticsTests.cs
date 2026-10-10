using TableSimulator.Application.Statistics;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Statistics;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Statistics;
using static TableSimulator.Tests.Protocols.Elen.ElenFrameTests;

namespace TableSimulator.Tests.Statistics;

/// <summary>
/// Statistika komunikacie v case: pocitadla liniek a tabul od posledneho odberu, najdlhsia pauza medzi spravami, zmeny
/// stavu liniek a zapis do databazy so zoskupenim po intervaloch.
/// </summary>
[TestClass]
public sealed class TrafficStatisticsTests
{
    private static readonly byte[] Row = ElenFrame.Build(5, 1, Hex("81 10 80 41"));

    private static Simulator Station(ManualTime time)
    {
        var sim = new Simulator(time);
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47101") { AutoLearn = false, Manufacturer = "ELEN10" });
        sim.SetBoard(new BoardConfig(1, 5, "ELEN10"));
        return sim;
    }

    [TestMethod]
    public void Odber_SpravyChybyBajtyAStavLinky()
    {
        var time = new ManualTime();
        var sim = Station(time);
        sim.SetLineStatus(1, LineStatus.Listening, null);
        sim.SetLineStatus(1, LineStatus.Connected, "127.0.0.1:5000");

        var replies = sim.Receive(1, Row).Sum(r => r.Bytes.Length);
        time.Advance(TimeSpan.FromSeconds(20));
        sim.Receive(1, Row);
        var bad = Row.ToArray();
        bad[^1] ^= 0x01;
        sim.Receive(1, bad);
        sim.Receive(1, ElenFrame.Build(9, 1, Hex("81 10 80 41")));
        sim.SetFault("1-5", new BoardFault(FaultKind.NoReply));
        sim.Receive(1, Row);
        sim.SetLineStatus(1, LineStatus.Listening, null);

        var sample = sim.TakeTraffic(100);

        var line = sample.Lines.Single();
        Assert.AreEqual(100, line.Minute);
        Assert.AreEqual(3, line.Ok);
        Assert.AreEqual(1, line.BadChecksum);
        Assert.AreEqual(1, line.NoBoard);
        Assert.AreEqual(1, line.Faults, "porucha neodpoveda");
        Assert.AreEqual(Row.Length * 4L + bad.Length, line.BytesIn);
        Assert.AreEqual(replies * 2L + 1, line.BytesOut, "dve ACK a NAK na zly sucet");
        Assert.AreEqual((1, 1), (line.Connections, line.Disconnections));
        CollectionAssert.AreEqual(new[] { LineStatus.Listening, LineStatus.Connected, LineStatus.Lost },
            sample.StatusChanges.Select(c => c.Status).ToArray());

        var board = sample.Boards.Single();
        Assert.AreEqual((1, 5), (board.Line, board.Address));
        Assert.AreEqual(4, board.Frames);
        Assert.AreEqual(1, board.Unanswered);
        Assert.AreEqual(20_000, board.MaxGapMs);

        // druhy odber: len co pribudlo
        Assert.IsTrue(sim.TakeTraffic(101).IsEmpty);
        sim.SetFault("1-5", BoardFault.None);
        sim.Receive(1, Row);
        var next = sim.TakeTraffic(102);
        Assert.AreEqual((1, 1), (next.Boards.Single().Frames, next.Boards.Single().Acks));
    }

    [TestMethod]
    public void Minuta_ZaokruhlenieAjPredRokom1970()
    {
        Assert.AreEqual(0, TrafficMinutes.Of(DateTimeOffset.FromUnixTimeMilliseconds(59_999)));
        Assert.AreEqual(-1, TrafficMinutes.Of(DateTimeOffset.FromUnixTimeMilliseconds(-1)));
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(120_000), TrafficMinutes.Start(2));
        Assert.AreEqual(1, TrafficRetention.BucketFor(TimeSpan.FromHours(1)));
        Assert.AreEqual(10, TrafficRetention.BucketFor(TimeSpan.FromDays(1)));
        Assert.AreEqual(60, TrafficRetention.BucketFor(TimeSpan.FromDays(7)));
        Assert.AreEqual(360, TrafficRetention.BucketFor(TimeSpan.FromDays(30)));
    }

    [TestMethod]
    public async Task Databaza_ZapisZoskupenieStavNaZaciatkuAMazanie()
    {
        var folder = Directory.CreateTempSubdirectory("tsstat");
        try
        {
            var file = Path.Combine(folder.FullName, "stat.db");
            await using (var setup = ServerDatabase.Open(file))
                await ServerDatabase.MigrateAsync(setup);

            static LineMinute Line(long minute, int ok) => new(1, minute, ok, 0, 0, 0, 0, ok, 0, ok * 10L, ok * 2L, 0, 0);
            static DateTimeOffset At(long minute) => TrafficMinutes.Start(minute);
            await using (var db = ServerDatabase.Open(file))
            {
                var repository = new TrafficStatisticsRepository(db);
                await repository.AddAsync(new TrafficSample([Line(1000, 2), Line(1005, 3)], [new BoardMinute(1, 5, 1000, 2, 2, 0, 0, 30_000)],
                    [new LineStatusChange(1, At(990), LineStatus.Connected, "x"), new LineStatusChange(1, At(1003), LineStatus.Lost, null)]), CancellationToken.None);
                // ta ista minuta znova (rozpracovana minuta pri restarte) - pripocita sa, pauza je vacsia z oboch
                await repository.AddAsync(new TrafficSample([Line(1005, 1)], [new BoardMinute(1, 5, 1000, 1, 0, 1, 0, 45_000)], []), CancellationToken.None);
            }

            await using (var db = ServerDatabase.Open(file))
            {
                var repository = new TrafficStatisticsRepository(db);
                var data = await repository.QueryAsync(1000, 1010, 10, null, CancellationToken.None);
                var line = data.Lines.Single();
                Assert.AreEqual((1000L, 6, 60L), (line.Minute, line.Ok, line.BytesIn));
                var board = data.Boards.Single();
                Assert.AreEqual((3, 1, 45_000L), (board.Frames, board.Naks, board.MaxGapMs!.Value));

                // od minuty 1001: stav na zaciatku je pripojenie z minuty 990, v obdobi odpojenie
                var later = await repository.QueryAsync(1001, 1010, 1, 1, CancellationToken.None);
                CollectionAssert.AreEqual(new[] { "connected", "lost" }, later.StatusChanges.Select(s => s.Status).ToArray());
                Assert.AreEqual(1005, later.Lines.Single().Minute);
                Assert.IsEmpty((await repository.QueryAsync(1000, 1010, 1, 2, CancellationToken.None)).Lines);

                // mazanie: posledna zmena stavu pred hranicou ostava
                Assert.AreEqual(3, await repository.DeleteOlderThanAsync(1004, CancellationToken.None));
                var rest = await repository.QueryAsync(1004, 1010, 1, null, CancellationToken.None);
                CollectionAssert.AreEqual(new[] { "lost" }, rest.StatusChanges.Select(s => s.Status).ToArray());
            }
        }
        finally
        {
            folder.Delete(true);
        }
    }
}
