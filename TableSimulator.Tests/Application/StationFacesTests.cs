using System.Text.Json;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Live;
using TableSimulator.Application.Settings;
using TableSimulator.Application.Station;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;
using ContractAlign = TableSimulator.Contracts.Faces.FaceAlign;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Cela tabul z dat stanice: predloha podla druhu tabule, popisy nad stlpcami a ich hodnoty, zdielanie predloh
/// a pridelenie tabuliam pri importe stanice.
/// </summary>
[TestClass]
public class StationFacesTests
{
    private sealed class TestLive : ILiveNotifier
    {
        public List<LiveMessage> Messages { get; } = [];

        public void Publish(LiveMessage message) => Messages.Add(message);
    }

    private sealed class TestSettings : ISettingsStore
    {
        public AppSettings Current { get; private set; } = new();

        public void Save(AppSettings settings) => Current = settings;
    }

    private sealed class NoVirtualBoards : IVirtualBoardRepository
    {
        public Task<IReadOnlyList<VirtualBoard>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VirtualBoard>>([]);

        public Task<VirtualBoard?> FindAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VirtualBoard?>(null);

        public Task<bool> SaveAsync(VirtualBoard board, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<VirtualImportCounts> ImportAsync(IReadOnlyList<VirtualBoard> boards, bool replace, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    // odchodova ELEN10 ako v katalogu: cas vpravo, druh a cislo vlaku vedla seba, ciel, kolaj a nastupiste uzke vpravo
    private static readonly StationFaceColumnDto[] DepartureColumns =
    [
        new(0, 0, 22, ContractAlign.Right, 9), new(0, 25, 34, ContractAlign.Left, 15), new(0, 35, 50, ContractAlign.Left, 16),
        new(0, 52, 135, ContractAlign.Left, 8), new(0, 130, 136, ContractAlign.Right, 20), new(0, 137, 152, ContractAlign.Right, 22),
        new(0, 154, 168, ContractAlign.Right, 24)
    ];

    private static StationFaceDto Departures(string station = "Horná Ves") =>
        new(StationBoardKind.Departures, 1, 7, 160, 10, DepartureColumns, [], [], station);

    private static StationFaceDto Platform(params string[] tracks) =>
        new(StationBoardKind.Platform, 1, 1, 280, 10, DepartureColumns, tracks, ["1"], "Horná Ves");

    private static List<FaceElement> Labels(BoardFace face) => face.Elements.Where(e => e is { Kind: FaceElementKind.Text, Style: "label", Anchor: FaceAnchor.Matrix }).ToList();

    [TestMethod]
    public void Odchodova_NadpisHodinyAPopisyNadStlpcami()
    {
        var face = StationFaces.Build(Departures(), false, StationFaceLanguage.Sk)!;

        Assert.AreEqual("Odchodová – Horná Ves", face.Face.Name);
        Assert.AreEqual("odchodova-Horná Ves", face.IdHint);
        Assert.IsEmpty(face.Values);
        Assert.IsTrue(face.Face.Elements.Any(e => e is { Kind: FaceElementKind.Text, Style: "title", Text: "ODCHODY / DEPARTURES" }));
        Assert.IsTrue(face.Face.Elements.Any(e => e is { Kind: FaceElementKind.Clock, Render: FaceRender.Display }));
        var labels = Labels(face.Face);
        // druh a cislo vlaku maju jeden popis; stlpec bez popisu (text riadka) sa vynecha
        CollectionAssert.AreEqual(new[] { "ODCHOD\nDEPARTURE", "VLAK\nTRAIN", "SMER\nDIRECTION", "KOĽAJ\nTRACK", "NÁSTUPIŠTE\nPLATFORM" },
            labels.Select(l => l.Text).ToArray());
        Assert.AreEqual(25, labels[1].X);
        // popisy nad maticou, v mieste nad nou a bez prekryvu - uzky popis vpravo konci na konci stlpca
        Assert.IsTrue(labels.All(l => l.Y < 0 && -l.Y <= face.Face.Margin.Top - 14));
        for (var i = 1; i < labels.Count; i++)
            Assert.IsLessThan(labels[i].X, labels[i - 1].X + labels[i - 1].MaxWidth, labels[i].Text);
        Assert.IsLessThanOrEqualTo(136, labels[3].X + labels[3].MaxWidth);
        Assert.IsGreaterThanOrEqualTo(8, labels[3].MaxWidth);

        var cs = StationFaces.Build(Departures(), false, StationFaceLanguage.Cs)!;
        Assert.IsTrue(cs.Face.Elements.Any(e => e.Text == "ODJEZDY / DEPARTURES"));
        Assert.AreEqual("ODJEZD\nDEPARTURE", Labels(cs.Face)[0].Text);
        Assert.AreEqual("Odjezdová – Horná Ves", cs.Face.Name);
    }

    [TestMethod]
    public void Nastupistna_PanelKolajeStanicaBezPopisov()
    {
        var face = StationFaces.Build(Platform("2", "2V"), false, StationFaceLanguage.Sk)!;

        Assert.AreEqual("2", face.Values[StationFaces.TrackKey]);
        Assert.AreEqual("Horná Ves", face.Values[StationFaces.StationKey]);
        Assert.IsTrue(face.Face.Margin.Left > 0);
        Assert.IsTrue(face.Face.Elements.Any(e => e is { Anchor: FaceAnchor.Left, Style: "number", Text: "{track}" }));
        Assert.IsTrue(face.Face.Elements.Any(e => e.Text == "{station}"));
        Assert.IsEmpty(Labels(face.Face));

        // bez kolaje len pas so stanicou a hodiny
        var bare = StationFaces.Build(Platform(), false, StationFaceLanguage.Sk)!;
        Assert.AreEqual(0, bare.Face.Margin.Left);
        Assert.IsFalse(bare.Values.ContainsKey(StationFaces.TrackKey));
    }

    [TestMethod]
    public void Podchodova_PanelNastupistaInakKolaje()
    {
        var info = new StationFaceDto(StationBoardKind.Underpass, 1, 2, 272, 16, DepartureColumns, ["1A", "1V"], ["1"], "Horná Ves");

        var face = StationFaces.Build(info, false, StationFaceLanguage.Sk)!;

        Assert.AreEqual("1", face.Values[StationFaces.PlatformKey]);
        Assert.IsTrue(face.Face.Elements.Any(e => e is { Style: "number", Text: "{platform}" }));
        Assert.IsNotEmpty(Labels(face.Face));
        // vyssie riadky (ELEN16) maju vacsie popisy
        Assert.IsGreaterThan(3.4, Labels(face.Face)[0].Size);

        var track = StationFaces.Build(info with { Platforms = [] }, false, StationFaceLanguage.Sk)!;
        Assert.AreEqual("1A", track.Values[StationFaces.TrackKey]);
        Assert.IsNull(StationFaces.Build(info with { Platforms = [], Tracks = [], Columns = [] }, false, StationFaceLanguage.Sk));
    }

    [TestMethod]
    public void ListovaAInyDruh_BezPopisovAleboBezCela()
    {
        var flaps = StationFaces.Build(Departures(), true, StationFaceLanguage.Sk)!;

        Assert.IsEmpty(Labels(flaps.Face));
        Assert.AreEqual(16, flaps.Face.Margin.Top);
        Assert.IsNull(StationFaces.Build(Departures() with { Kind = StationBoardKind.Other }, false, StationFaceLanguage.Sk));
    }

    [TestMethod]
    [DataRow("2|2V", "2")]
    [DataRow("1A|1V", "1A")]
    [DataRow("3Z|3", "3")]
    [DataRow("", "")]
    public void Hodnota_ZaciatokVsetkychInakPrva(string values, string expected) =>
        Assert.AreEqual(expected, StationFaces.Pick(values.Split('|', StringSplitOptions.RemoveEmptyEntries)));

    [TestMethod]
    public async Task Import_ZdielanePredlohyLenPouziteAVolbaPreExistujuce()
    {
        var simulator = new Simulator();
        // linka cez ruru - simulator sa k nej len pokusa pripojit, neobsadi port
        simulator.SetLine(new LineConfig(3, 4, @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N")) { AutoLearn = false });
        await using var transport = new TransportManager(simulator);
        var faces = new MemoryFaceRepository();
        var live = new TestLive();
        var importer = new StationImporter(simulator, transport, new TestSettings(), new NoVirtualBoards(), faces, live);
        var podchod = new StationFaceDto(StationBoardKind.Underpass, 1, 2, 272, 16, DepartureColumns, [], ["4"], "Horná Ves");
        var request = new StationImportRequest([],
        [
            new StationBoardDto(3, 5, "ELEN16", "Odchody 1") { Face = Departures() },
            new StationBoardDto(3, 6, "ELEN16", "Odchody 2") { Face = Departures() },
            new StationBoardDto(3, 7, "ELEN16", "Nastupistna 2") { Face = Platform("2", "2V") },
            new StationBoardDto(3, 8, "ELEN16", "Bez cela"),
            // linka, ktoru simulator nema - jej predloha sa neulozi
            new StationBoardDto(9, 1, "ELEN16", "Podchod") { Face = podchod }
        ]);

        var result = await importer.ImportAsync(request, CancellationToken.None);

        Assert.AreEqual(2, result.FacesAdded);
        Assert.AreEqual(3, result.FacesAssigned);
        Assert.HasCount(2, faces.Faces);
        Assert.IsTrue(live.Messages.OfType<FacesMessage>().Any());
        var first = simulator.Read(s => s.FindBoard("3-5")!.Profile);
        Assert.AreEqual("odchodova-horna-ves", first.Face);
        Assert.AreEqual(first.Face, simulator.Read(s => s.FindBoard("3-6")!.Profile.Face));
        // rozmer novej tabule z katalogu - stlpce siahaju za sirku segmentu
        Assert.AreEqual(168, first.Width);
        Assert.AreEqual(7, first.Rows);
        var platform = simulator.Read(s => s.FindBoard("3-7")!.Profile);
        Assert.AreEqual("nastupistna-horna-ves", platform.Face);
        Assert.AreEqual("2", platform.FaceValues![StationFaces.TrackKey]);
        Assert.IsNull(simulator.Read(s => s.FindBoard("3-8")!.Profile.Face));
        var saved = BoardFaces.Deserialize(faces.Faces["odchodova-horna-ves"]);
        Assert.AreEqual("Odchodová – Horná Ves", saved.Name);

        // opakovany import: existujuce predlohy sa pouziju, tabule bez cela ho dostanu len na vyziadanie
        var again = request with { Boards = [.. request.Boards.Take(3), new StationBoardDto(3, 8, "ELEN16", "Bez cela") { Face = Departures() }] };
        var kept = await importer.ImportAsync(again, CancellationToken.None);
        Assert.AreEqual(0, kept.FacesAdded);
        Assert.AreEqual(0, kept.FacesAssigned);
        Assert.IsNull(simulator.Read(s => s.FindBoard("3-8")!.Profile.Face));

        simulator.SetBoard(simulator.Read(s => s.ToConfig()).Boards.Single(b => b.Address == 6) is { } six
            ? six with { Profile = six.Profile with { Face = "odjezdy" } }
            : throw new InvalidOperationException());
        var existing = await importer.ImportAsync(again with { FacesForExisting = true }, CancellationToken.None);
        Assert.AreEqual(0, existing.FacesAdded);
        Assert.AreEqual(1, existing.FacesAssigned);
        Assert.AreEqual("odchodova-horna-ves", simulator.Read(s => s.FindBoard("3-8")!.Profile.Face));
        // celo vybrate obsluhou ostane
        Assert.AreEqual("odjezdy", simulator.Read(s => s.FindBoard("3-6")!.Profile.Face));
        Assert.HasCount(2, faces.Faces);
    }

    [TestMethod]
    public void Kontrakt_KluceAEnumy()
    {
        var json = JsonSerializer.SerializeToElement(new StationBoardDto(1, 2, "ELEN") { Face = Departures() }, Web);
        var face = json.GetProperty("face");

        Assert.AreEqual("departures", face.GetProperty("kind").GetString());
        Assert.AreEqual("right", face.GetProperty("columns")[0].GetProperty("align").GetString());
        Assert.AreEqual("cs", JsonSerializer.SerializeToElement(StationFaceLanguage.Cs).GetString());
    }
}
