using TableSimulator.Application.Boards;
using TableSimulator.Application.Config;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Predlohy ciel tabul: vstavane predlohy, prevod starsich hlaviciek na predlohy, prikazy a prenos v subore konfiguracie.
/// </summary>
[TestClass]
public class BoardFacesTests
{
    private sealed class TestLive : ILiveNotifier
    {
        public List<LiveMessage> Messages { get; } = [];

        public void Publish(LiveMessage message) => Messages.Add(message);
    }

    private static BoardHeader Header(string text) => new()
    {
        Height = 14,
        Texts = [new HeaderText(4, 2, 10, text)],
        Clock = new HeaderClock(160, 2, 10)
    };

    private static BoardConfig Board(int address, BoardProfile profile, string name = "") =>
        new(1, address, "ELEN") { Name = name, Profile = profile };

    [TestMethod]
    public void Vstavane_PlatneKluceAPresliOverenim()
    {
        var validator = new SetFaceCommandValidator();

        Assert.IsGreaterThanOrEqualTo(8, BoardFaces.BuiltIn.Count);
        Assert.HasCount(BoardFaces.BuiltIn.Count, BoardFaces.BuiltIn.Select(f => f.Id).Distinct());
        foreach (var face in BoardFaces.BuiltIn)
        {
            Assert.IsTrue(BoardFaces.IsValidId(face.Id), face.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(face.Name), face.Id);
            Assert.IsNotEmpty(face.Elements, face.Id);
            var result = validator.Validate(new SetFaceCommand(face.Id, DtoMapper.ToDto(face, true)));
            Assert.IsTrue(result.IsValid, $"{face.Id}: {string.Join("; ", result.Errors)}");
        }

        var platform = BoardFaces.FindBuiltIn("kolaj-vlavo")!;
        Assert.AreEqual(36, platform.Margin.Left);
        Assert.IsTrue(platform.Elements.Any(e => e is { Kind: FaceElementKind.Text, Anchor: FaceAnchor.Left, Text: "{track}" }));
        Assert.AreEqual(3, BoardFaces.FindBuiltIn("odjezd-lcd")!.RowGroup);
    }

    [TestMethod]
    public void ZHlavicky_PasTextyAHodinyNaRovnakychMiestach()
    {
        var face = BoardFaces.FromHeader(Header("Odchody / Departures"), "f", "F");

        Assert.AreEqual(new FaceMargin(Top: 14), face.Margin);
        Assert.HasCount(3, face.Elements);
        Assert.AreEqual(new FaceElement { Kind = FaceElementKind.Rect, Height = 14, Fill = "band" }, face.Elements[0]);
        Assert.AreEqual(new FaceElement { Kind = FaceElementKind.Text, X = 4, Y = 2, Size = 10, Text = "Odchody / Departures" }, face.Elements[1]);
        Assert.AreEqual(new FaceElement { Kind = FaceElementKind.Clock, X = 160, Y = 2, Size = 10 }, face.Elements[2]);
        Assert.AreEqual(FaceClockFormat.HourMinutePadded, face.Elements[2].Format);
    }

    [TestMethod]
    public async Task Prevod_RovnakeHlavickyJednaPredloha()
    {
        var repository = new MemoryFaceRepository();
        var config = new SimulatorConfig
        {
            Lines = [new LineConfig(1, 4, "TCP://127.0.0.1:47001")],
            Boards =
            [
                Board(1, new BoardProfile { Header = Header("Odchody") }, "Hala"),
                Board(2, new BoardProfile { Header = Header("Odchody") }),
                Board(3, new BoardProfile { Header = Header("Príchody") }),
                Board(4, new BoardProfile { Header = Header("Iné"), Face = "odchody" }),
                Board(5, new BoardProfile())
            ]
        };

        var (result, changed) = await new FaceHeaders(repository).AbsorbAsync(config, CancellationToken.None);

        Assert.IsTrue(changed);
        Assert.IsTrue(result.Boards.All(b => b.Profile.Header is null));
        CollectionAssert.AreEqual(new[] { "hlavicka-1", "hlavicka-1", "hlavicka-2", "odchody", null }, result.Boards.Select(b => b.Profile.Face).ToArray());
        Assert.HasCount(2, repository.Faces);
        Assert.AreEqual("Hala", repository.Faces["hlavicka-1"].Name);
        Assert.AreEqual("1-3", repository.Faces["hlavicka-2"].Name);
        Assert.AreEqual("Odchody", BoardFaces.Deserialize(repository.Faces["hlavicka-1"]).Elements[1].Text);

        // druhy prevod najde predlohy podla obsahu
        var again = await new FaceHeaders(repository).AbsorbAsync(Board(6, new BoardProfile { Header = Header("Príchody") }), null, null, CancellationToken.None);
        Assert.AreEqual("hlavicka-2", again.Profile.Face);
        Assert.HasCount(2, repository.Faces);
        var (_, unchanged) = await new FaceHeaders(repository).AbsorbAsync(result, CancellationToken.None);
        Assert.IsFalse(unchanged);
    }

    [TestMethod]
    public async Task Prevod_KlucZNapovedy()
    {
        var repository = new MemoryFaceRepository();
        var headers = new FaceHeaders(repository);

        var first = await headers.AbsorbAsync(Board(1, new BoardProfile { Header = Header("A") }), "Sip SDO 5x320", "monitor-Sip_SDO 5x320", CancellationToken.None);
        var other = await headers.AbsorbAsync(Board(2, new BoardProfile { Header = Header("B") }), "Iný", "monitor-Sip_SDO 5x320", CancellationToken.None);

        Assert.AreEqual("monitor-sip-sdo-5x320", first.Profile.Face);
        Assert.AreEqual("monitor-sip-sdo-5x320-1", other.Profile.Face);
        Assert.AreEqual("Sip SDO 5x320", repository.Faces["monitor-sip-sdo-5x320"].Name);
    }

    [TestMethod]
    public async Task UlozenieTabule_HlavickaSaStanePredlohouANeznamaPredlohaChyba()
    {
        var simulator = new Simulator();
        simulator.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001"));
        var repository = new MemoryFaceRepository();
        var handler = new SetBoardCommandHandler(simulator, repository, new FaceHeaders(repository));
        var header = new BoardHeaderDto { Height = 12, Texts = [new HeaderTextDto(2, 1, 9, "Odchody")] };

        var saved = await handler.Handle(new SetBoardCommand(1, 7, new SetBoardRequest { Manufacturer = "ELEN", Profile = new BoardProfileDto { Header = header } }),
            CancellationToken.None);
        var unknown = await handler.Handle(new SetBoardCommand(1, 8, new SetBoardRequest { Manufacturer = "ELEN", Profile = new BoardProfileDto { Face = "nie-je" } }),
            CancellationToken.None);
        var builtIn = await handler.Handle(new SetBoardCommand(1, 9, new SetBoardRequest
        {
            Manufacturer = "ELEN",
            Profile = new BoardProfileDto { Face = "kolaj-vlavo", FaceValues = new Dictionary<string, string> { ["track"] = "1", ["station"] = "" } }
        }), CancellationToken.None);

        Assert.IsTrue(saved.IsSuccess);
        var board = simulator.Read(s => s.FindBoard("1-7"))!;
        Assert.IsNull(board.Profile.Header);
        Assert.AreEqual("hlavicka-1", board.Profile.Face);
        Assert.AreEqual("face.notFound", unknown.Error.Code);
        Assert.IsTrue(builtIn.IsSuccess);
        // prazdne hodnoty sa neukladaju
        CollectionAssert.AreEquivalent(new[] { "track" }, simulator.Read(s => s.FindBoard("1-9"))!.Profile.FaceValues!.Keys.ToArray());
    }

    [TestMethod]
    public async Task Prikazy_VstavanaSaNedaZmenitPouzivanaOdstranit()
    {
        var simulator = new Simulator();
        simulator.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001"));
        var repository = new MemoryFaceRepository();
        var live = new TestLive();
        var set = new SetFaceCommandHandler(repository, live);
        var delete = new DeleteFaceCommandHandler(repository, simulator, live);
        var face = DtoMapper.ToDto(BoardFaces.FindBuiltIn("odchody")!, false) with { Name = "  Moja  " };

        Assert.AreEqual("face.builtIn", (await set.Handle(new SetFaceCommand("odchody", face), CancellationToken.None)).Error.Code);
        Assert.IsTrue((await set.Handle(new SetFaceCommand("moja", face), CancellationToken.None)).IsSuccess);
        Assert.AreEqual("Moja", repository.Faces["moja"].Name);
        simulator.SetBoard(Board(3, new BoardProfile { Face = "moja" }));

        Assert.AreEqual("face.inUse", (await delete.Handle(new DeleteFaceCommand("moja"), CancellationToken.None)).Error.Code);
        Assert.AreEqual("face.builtIn", (await delete.Handle(new DeleteFaceCommand("odchody"), CancellationToken.None)).Error.Code);
        simulator.SetBoard(Board(3, new BoardProfile()));
        Assert.IsTrue((await delete.Handle(new DeleteFaceCommand("moja"), CancellationToken.None)).IsSuccess);
        Assert.AreEqual("face.notFound", (await delete.Handle(new DeleteFaceCommand("moja"), CancellationToken.None)).Error.Code);
        Assert.HasCount(2, live.Messages.OfType<FacesMessage>());
    }

    [TestMethod]
    public async Task SuborKonfiguracie_VlastnePredlohyIduSTabulami()
    {
        var source = new Simulator();
        source.SetLine(new LineConfig(1, 4, "TCP://127.0.0.1:47001"));
        var faces = new MemoryFaceRepository();
        var custom = BoardFaces.FromHeader(Header("Odchody"), "moja", "Moja");
        await faces.SaveAsync(new FaceRecord("moja", "Moja", BoardFaces.Serialize(custom)), CancellationToken.None);
        await faces.SaveAsync(new FaceRecord("nepouzita", "Nepoužitá", BoardFaces.Serialize(custom with { Id = "nepouzita" })), CancellationToken.None);
        source.SetBoard(Board(1, new BoardProfile { Face = "moja", FaceValues = new Dictionary<string, string> { ["track"] = "2" } }));
        source.SetBoard(Board(2, new BoardProfile { Face = "odchody" }));

        var file = (await new ExportConfigQueryHandler(source, faces).Handle(new ExportConfigQuery(), CancellationToken.None)).Value;
        var exported = SimulatorConfigJson.Deserialize(file.Content);
        CollectionAssert.AreEqual(new[] { "moja" }, exported.Faces.Select(f => f.Id).ToArray());

        var target = new Simulator();
        await using var transport = new TransportManager(target);
        var targetFaces = new MemoryFaceRepository();
        var live = new TestLive();
        var import = new ImportConfigCommandHandler(target, transport, targetFaces, new FaceHeaders(targetFaces), live);
        var result = await import.Handle(new ImportConfigCommand(file.Content, true), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(BoardFaces.Content(custom), BoardFaces.Content(BoardFaces.Deserialize(targetFaces.Faces["moja"])));
        Assert.AreEqual("2", target.Read(s => s.FindBoard("1-1"))!.Profile.FaceValues!["track"]);
        Assert.HasCount(1, live.Messages.OfType<FacesMessage>());
    }
}
