using System.Net;
using System.Net.Http.Json;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Recordings;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Tests.Server;

/// <summary>Porovnanie zaznamov cez API: vysledok po tabuliach s krokmi a tabula zo zaznamu v case.</summary>
[TestClass]
[DoNotParallelize]
public sealed class RecordingComparisonApiTests
{
    private static byte[] Row(int address, int row, string text) =>
        ElenFrame.Build(address, 1, [(byte)(0x80 | row), 0x10, 0x80, 0x1B, 0x52, .. Encodings.Win1250.GetBytes(text)]);

    /// <summary>Subor zaznamu s riadkami pre tabule linky 3.</summary>
    private static byte[] Recording(params (double Time, byte[] Bytes)[] frames)
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(3, 4, "TCP://127.0.0.1:47003") { Manufacturer = "ELEN10" });
        var header = new RecordingHeader(RecordingFile.FormatName, RecordingFile.Version, DateTimeOffset.UnixEpoch, sim.ToConfig(), null);
        var file = new RecordingFile(header, [.. frames.Select(f => new RecordingEntry(f.Time, 3, FrameDirection.Received, f.Bytes))]);
        using var stream = new MemoryStream();
        file.Write(stream);
        return stream.ToArray();
    }

    private static async Task Upload(HttpClient client, string name, byte[] content)
    {
        var response = await client.PostAsync($"/api/recordings/upload?name={name}.tsrec", new ByteArrayContent(content));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Porovnanie_PoTabuliachAStavVCase()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        await Upload(client, "pred", Recording((0, Row(3, 1, "Os 1234 Žilina")), (10, Row(3, 1, "Os 1236 Košice")), (12, Row(5, 1, "Pokladňa"))));
        await Upload(client, "po", Recording((3, Row(3, 1, "Os 1234 Žilina")), (14, Row(3, 1, "Os 1238 Prešov")), (15, Row(7, 1, "Nová"))));

        var comparison = await client.GetFromJsonAsync<RecordingComparisonDto>("/api/recordings/compare?a=pred&b=po");

        Assert.AreEqual("pred", comparison!.A.Name);
        CollectionAssert.AreEqual(new[] { "3-3", "3-5", "3-7" }, comparison.Boards.Select(b => b.Id).ToArray());
        var board = comparison.Boards[0];
        Assert.AreEqual(BoardDifference.Different, board.Result);
        CollectionAssert.AreEqual(new[] { ComparisonStepKind.Same, ComparisonStepKind.Changed }, board.Steps.Select(s => s.Kind).ToArray());
        Assert.AreEqual("Os 1236 Košice", board.Steps[1].A!.Rows[^1]);
        Assert.AreEqual("Os 1238 Prešov", board.Steps[1].B!.Rows[^1]);
        Assert.AreEqual(14d, board.Steps[1].B!.Time);
        Assert.AreEqual(BoardDifference.OnlyA, comparison.Boards[1].Result);
        Assert.AreEqual(BoardDifference.OnlyB, comparison.Boards[2].Result);

        var before = await client.GetFromJsonAsync<BoardDto>("/api/recordings/pred/boards/3-3?time=5");
        Assert.AreEqual("Os 1234 Žilina", before!.Display.Rows.Single().Elements.Single().Run!.Text);

        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/recordings/compare?a=pred&b=neexistuje")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/recordings/pred/boards/9-9?time=5")).StatusCode);
    }
}
