using System.Text.Json;
using System.Text.Json.Serialization;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Frames;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Signage;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Tvar JSON kontraktov, ktory cita webove rozhranie (web/src/api.ts) a GVDEditor: kluce, hodnoty enumov, sprava WebSocketu.
/// </summary>
[TestClass]
public class ContractsJsonTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static JsonElement Json<T>(T value) => JsonSerializer.SerializeToElement(value, Web);

    [TestMethod]
    public void Tabula_KluceAEnumy()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001") { AutoLearn = false });
        sim.SetBoard(new BoardConfig(1, 5, "ELEN16") { Fault = new BoardFault(Core.Boards.FaultKind.NoReply, 0, 0) });

        var board = Json(sim.Read(s => DtoMapper.ToDto(s.FindBoard("1-5")!)));

        Assert.AreEqual("noReply", board.GetProperty("fault").GetProperty("kind").GetString());
        Assert.AreEqual("elen", board.GetProperty("display").GetProperty("family").GetString());
        Assert.AreEqual(192, board.GetProperty("profile").GetProperty("width").GetInt32());
        Assert.AreEqual(JsonValueKind.Null, board.GetProperty("secondAddress").ValueKind);
        Assert.IsTrue(board.TryGetProperty("mismatch", out _));
    }

    [TestMethod]
    public void Linka_RezimAStav()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001") { Mode = Core.Lines.LineMode.Monitor });

        var line = Json(sim.Read(s => DtoMapper.ToDto(s.FindLine(1)!, "10.0.0.5")));

        Assert.AreEqual("monitor", line.GetProperty("mode").GetString());
        Assert.AreEqual("stopped", line.GetProperty("status").GetString());
        Assert.AreEqual("1=TCP://10.0.0.5:47001", line.GetProperty("inissTablePort").GetString());
    }

    [TestMethod]
    public void Zaznam_SmerAVysledok()
    {
        var record = new FrameRecord(7, DateTimeOffset.UnixEpoch, 1, Core.Protocols.FrameDirection.Sent, 5, [0x06], Core.Protocols.FrameStatus.Fault,
            new FrameInfo("ack"));

        var frame = Json(DtoMapper.ToDto(record));

        Assert.AreEqual("tx", frame.GetProperty("direction").GetString());
        Assert.AreEqual("fault", frame.GetProperty("status").GetString());
        Assert.AreEqual("06", frame.GetProperty("hex").GetString());
    }

    [TestMethod]
    public void SpravaWebSocketu_DruhVoVlastnostiType()
    {
        var json = JsonSerializer.Serialize<LiveMessage>(new DesignMessage("lcd-dark"), Web);

        Assert.AreEqual("""{"type":"design","id":"lcd-dark"}""", json);
        Assert.AreEqual("""{"type":"structure"}""", JsonSerializer.Serialize<LiveMessage>(new StructureMessage(), Web));
        Assert.AreEqual("""{"type":"virtual","sources":["KosiceO"],"definitions":false}""",
            JsonSerializer.Serialize<LiveMessage>(new VirtualMessage(["KosiceO"], false), Web));
        Assert.AreEqual("""{"type":"signage"}""", JsonSerializer.Serialize<LiveMessage>(new SignageChangedMessage(), Web));
    }

    [TestMethod]
    public void InformacnePanely_KluceEnumovADatumy()
    {
        var message = new SetSignageMessageRequest("Oznam", "", SignageMessageKind.Text, 15, null, null,
            [new SignageParagraphDto("Text", "#ffffff", SignageTextSize.Medium, SignageAlign.Right)], "#000000", null, null);
        var playlist = new SetSignagePlaylistRequest("Ráno", SignagePeriod.Weekly, new DateOnly(2026, 10, 1), null, [1, 5], 360, 540, []);

        var m = Json(message);
        var p = Json(playlist);

        Assert.AreEqual("text", m.GetProperty("kind").GetString());
        Assert.AreEqual("medium", m.GetProperty("paragraphs")[0].GetProperty("size").GetString());
        Assert.AreEqual("right", m.GetProperty("paragraphs")[0].GetProperty("align").GetString());
        Assert.AreEqual("weekly", p.GetProperty("period").GetString());
        Assert.AreEqual("2026-10-01", p.GetProperty("start").GetString());
        Assert.AreEqual(JsonValueKind.Null, p.GetProperty("end").ValueKind);
        Assert.AreEqual("noGroup", JsonSerializer.SerializeToElement(SignageOffline.NoGroup, Web).GetString());
    }

    [TestMethod]
    public void PoziadavkaLinky_ChybajuceHodnotyPredvolene()
    {
        var request = JsonSerializer.Deserialize<SetLineRequest>("""{"tableClass":5,"endpoint":"TCP://0.0.0.0:47002","mode":"monitor"}""", Web)!;

        var config = RequestMapper.ToConfig(request, 2);

        Assert.AreEqual(Core.Lines.LineMode.Monitor, config.Mode);
        Assert.IsTrue(config.AutoLearn);
        Assert.IsNull(config.Mirror);
    }

    [TestMethod]
    public void Profil_TamASpatBezStraty()
    {
        var profile = new BoardProfile
        {
            Width = 512, Rows = 6, RowHeight = 12, FontFile = "f.bin", FontMap = new Dictionary<string, int> { ["face1"] = 2 },
            AlternationMs = 3000, BlinkOnMs = 400, BlinkOffMs = 300, ScrollSpeed = 40, Monochrome = true, SwapRedGreen = true, MovingFont = 0x51,
            AutoSize = true,
            Header = new BoardHeader { Height = 20, Texts = [new HeaderText(1, 2, 8, "ODCHODY")], Clock = new HeaderClock(400, 4, 8) }
        };
        var options = new JsonSerializerOptions(Web) { Converters = { new JsonStringEnumConverter() } };

        var back = RequestMapper.ToProfile(DtoMapper.ToDto(profile));

        Assert.AreEqual(JsonSerializer.Serialize(profile, options), JsonSerializer.Serialize(back, options));
    }

    [TestMethod]
    public void Porucha_TamASpat()
    {
        foreach (var kind in Enum.GetValues<Core.Boards.FaultKind>())
        {
            var fault = new BoardFault(kind, 100, 8);
            Assert.AreEqual(fault, RequestMapper.ToFault(DtoMapper.ToDto(fault)));
        }

        Assert.AreEqual(Contracts.Boards.FaultKind.Status, DtoMapper.ToDto(new BoardFault(Core.Boards.FaultKind.Status, 0, 1)).Kind);
    }
}
