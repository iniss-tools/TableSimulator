using TableSimulator.Application.Devices;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Station;
using ToolsCore.Iniss.Tools;
using ToolsCore.Iniss2.Devices;
using ToolsCore.Iniss2.Tables;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Ustredne AK Signal servera INISS2: priradenie svetiel a audio liniek k linkam podla pravidiel servera a dlzky zvukov
/// z banky servera.
/// </summary>
[TestClass]
public sealed class Iniss2DevicesTests
{
    private static readonly HashSet<int> Stations = [5400010];

    private static CommLine Line(string key, string protocol) => new(key, CommLineType.TcpClient, protocol, "10.0.0.1:1");

    private static Iniss2Devices.Result Resolve(IReadOnlyList<CommLine> lines, params LightDefinition[] lights) =>
        Iniss2Devices.Resolve(lines, lights, [], Stations);

    private static InissProblemDto Problem(Iniss2Devices.Result result) => result.Problems.Single();

    [TestMethod]
    public void Svetlo_BezLinkyJedinaVhodnaLinka()
    {
        var result = Resolve([Line("Tabule", "ELEN"), Line("Svetlo", "AKSIGNAL")], new LightDefinition(5400010, "A", LightsXml.AkSignal, "") { Address = 2 });

        Assert.AreEqual(2, result.For("Svetlo").Single().Address);
        Assert.IsEmpty(result.Problems);
    }

    [TestMethod]
    public void Svetlo_BezLinkyViacAleboZiadna()
    {
        var ambiguous = Resolve([Line("A", "AKSIGNAL"), Line("B", "MASTER_LIGHT")], new LightDefinition(1, "S", LightsXml.AkSignal, "") { Address = 1 });
        Assert.AreEqual(InissProblemCode.DeviceLineAmbiguous, Problem(ambiguous).Code);
        CollectionAssert.AreEqual(new[] { "S", "AKSIGNAL" }, Problem(ambiguous).Args.ToArray());

        var none = Resolve([Line("A", "AKSIGNAL")], new LightDefinition(1, "U", LightsXml.AkSignalUh, ""));
        Assert.AreEqual(InissProblemCode.DeviceNoLine, Problem(none).Code);
        CollectionAssert.AreEqual(new[] { "U", "AKSIGNALUH" }, Problem(none).Args.ToArray());
    }

    [TestMethod]
    public void Svetlo_LinkaPodlaKlucaPresne()
    {
        var unknown = Resolve([Line("Svetlo", "AKSIGNAL")], new LightDefinition(1, "S", LightsXml.AkSignal, "svetlo") { Address = 1 });
        Assert.AreEqual(InissProblemCode.DeviceUnknownLine, Problem(unknown).Code);

        var protocol = Resolve([Line("Svetlo", "AKSIGNALUH")], new LightDefinition(1, "S", LightsXml.AkSignal, "Svetlo") { Address = 1 });
        Assert.AreEqual(InissProblemCode.DeviceWrongProtocol, Problem(protocol).Code);
        CollectionAssert.AreEqual(new[] { "S", "Svetlo", "AKSIGNALUH" }, Problem(protocol).Args.ToArray());
    }

    [TestMethod]
    public void Svetlo_PodriadenyServerANeznamyDruh()
    {
        var master = Resolve([Line("Podr", "MASTER_LIGHT")], new LightDefinition(1, "S", LightsXml.AkSignal, "Podr") { Address = 1 });
        Assert.AreEqual(InissProblemCode.MasterLight, Problem(master).Code);
        Assert.AreEqual(InissProblemSeverity.Warning, Problem(master).Severity);
        Assert.IsEmpty(master.Controllers);

        var type = Resolve([Line("Svetlo", "AKSIGNAL")], new LightDefinition(1, "S", "TORNZ", "Svetlo"));
        Assert.AreEqual(InissProblemCode.DeviceType, Problem(type).Code);
    }

    [TestMethod]
    public void Audio_LenSFrontouAStanicouZDirListu()
    {
        AudioLineDefinition[] audio =
        [
            new(5400010, "Bez fronty", AudioXml.AkSignalUh, "Uh"),
            new(5499999, "Ina stanica", AudioXml.AkSignalUh, "Uh") { Queue = "F" },
            new(0, "Bez stanice", AudioXml.AkSignalUh, "Uh") { Queue = "F" },
            new(5400010, "Zvukova karta", AudioXml.None, "Uh") { Queue = "F" },
            new(5400010, "Bez linky", AudioXml.AkSignal, "") { Queue = "F", Address = 1 }
        ];

        var result = Iniss2Devices.Resolve([Line("Uh", "AKSIGNALUH")], [], audio, Stations);

        Assert.AreEqual("Bez stanice", result.For("Uh").Single().Audio.Single().Name);
        Assert.AreEqual(InissProblemCode.DeviceNoLine, Problem(result).Code);
    }

    [TestMethod]
    public void DlzkyZvukov_ZBankyServera()
    {
        var root = Directory.CreateTempSubdirectory("tssounds");
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "CZ", "vlak"));
            using (var file = File.Create(Path.Combine(root.FullName, "CZ", "vlak", "1234.EWA")))
                EwaCodec.Encode(new MemoryStream(Wav(16_000)), file, key: 0x33);
            File.WriteAllText(Path.Combine(root.FullName, "CZ", "zly.EWA"), "nie je wav");
            var lengths = new SoundLengths(root.FullName);

            Assert.AreEqual(TimeSpan.FromSeconds(2), lengths.Length("CZ/vlak/1234"));
            Assert.AreEqual(TimeSpan.FromSeconds(2), lengths.Length("CZ\\vlak\\1234.EWA"));
            Assert.IsNull(lengths.Length("CZ/vlak/9999"));
            Assert.IsNull(lengths.Length("CZ/zly"));
            Assert.IsNull(lengths.Length("../mimo"), "len subory v banke");
        }
        finally
        {
            root.Delete(true);
        }
    }

    /// <summary>WAV PCM 8 kHz mono 8 bit - 8000 bajtov za sekundu.</summary>
    private static byte[] Wav(int samples)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("RIFF"u8.ToArray());
        w.Write(36 + samples);
        w.Write("WAVEfmt "u8.ToArray());
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(8000);
        w.Write(8000);
        w.Write((short)1);
        w.Write((short)8);
        w.Write("data"u8.ToArray());
        w.Write(samples);
        w.Write(new byte[samples]);
        w.Flush();
        return ms.ToArray();
    }
}
