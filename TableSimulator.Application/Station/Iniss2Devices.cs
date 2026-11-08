using System.Globalization;
using TableSimulator.Contracts.Station;
using ToolsCore.Iniss2.Devices;
using ToolsCore.Iniss2.Tables;

namespace TableSimulator.Application.Station;

/// <summary>
/// Ustredne AK Signal servera INISS2: svetla z Lights.xml a audio linky z Audio.xml priradene k linkam a adresam podla
/// pravidiel servera (docs/iniss2/formaty-suborov/lights-xml.mdx, audio-xml.mdx). Co by server pri starte odmietol,
/// je zistenie, nie ustredna.
/// </summary>
public static class Iniss2Devices
{
    /// <summary>Protokol linky ustredne AK Signal.</summary>
    public const string AkSignal = "AKSIGNAL";

    /// <summary>Protokol linky ustredne AK Signal UH.</summary>
    public const string AkSignalUh = "AKSIGNALUH";

    /// <summary>Protokol linky k podriadenemu serveru so svetlami - simulator ho nehra.</summary>
    public const string MasterLight = "MASTER_LIGHT";

    /// <summary>Adresa ustredne AK Signal UH v simulatore - protokol adresu nema.</summary>
    public const int UhAddress = 0;

    /// <summary>Ustredne podla kluca linky a zistenia, ktore sa linke priradit nedaju.</summary>
    /// <param name="Controllers">ustredne podla kluca linky v poradi adries</param>
    /// <param name="Problems">zistenia</param>
    public sealed record Result(IReadOnlyDictionary<string, IReadOnlyList<InissControllerDto>> Controllers, IReadOnlyList<InissProblemDto> Problems)
    {
        /// <summary>Ustredne linky.</summary>
        public IReadOnlyList<InissControllerDto> For(string line) => Controllers.GetValueOrDefault(line, []);
    }

    /// <summary>Priradi svetla a audio linky k linkam a adresam ustredni.</summary>
    /// <param name="lines">linky servera</param>
    /// <param name="lights">svetla z Lights.xml</param>
    /// <param name="audio">audio linky z Audio.xml</param>
    /// <param name="stations">cisla stanic z DirList.xml - audio linku inej stanice server vynecha</param>
    public static Result Resolve(IReadOnlyList<CommLine> lines, IReadOnlyList<LightDefinition> lights, IReadOnlyList<AudioLineDefinition> audio,
        IReadOnlySet<int> stations)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(lights);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(stations);

        var problems = new List<InissProblemDto>();
        var found = new Dictionary<(string Line, int Address), (string Protocol, List<StationLightDto> Lights, List<StationAudioDto> Audio)>();

        foreach (var light in lights)
        {
            string[] protocols = light.Type switch
            {
                LightsXml.AkSignal => [AkSignal, MasterLight],
                LightsXml.AkSignalUh => [AkSignalUh],
                _ => []
            };
            if (protocols.Length == 0)
            {
                problems.Add(Error(InissProblemCode.DeviceType, light.Name, light.Type));
                continue;
            }

            var line = light.Line.Length > 0 ? Find(lines, light.Line, light.Name, protocols, problems) : Only(lines, light.Name, protocols, problems);
            if (line is null || IsMaster(line, light.Name, problems))
                continue;

            var address = UhAddress;
            if (line.Protocol == AkSignal)
            {
                if (light.Address is < 1 or > 255)
                {
                    problems.Add(Error(InissProblemCode.DeviceAddress, light.Name, Num(light.Address)));
                    continue;
                }

                address = light.Address;
            }

            Entry(found, line, address).Lights.Add(new StationLightDto(light.Name, light.Station)
            {
                BeforeArrival = light.BeforeArrival, BeforeDeparture = light.BeforeDeparture, AfterDeparture = light.AfterDeparture,
                AfterArrival = light.AfterArrival, MinOff = light.MinOff
            });
        }

        foreach (var item in audio)
        {
            // server zalozi ovladac len s frontou (pri AKSIGNAL aj so zvukovym zariadenim) a len pre stanicu z DirList.xml
            var uh = item.Output == AudioXml.AkSignalUh;
            if (!uh && item.Output != AudioXml.AkSignal)
                continue;
            if (item.Station != 0 && !stations.Contains(item.Station))
                continue;
            if (item.Queue.Length == 0 && (uh || item.Wave.Length == 0))
                continue;

            string[] protocols = uh ? [AkSignalUh] : [AkSignal, MasterLight];
            if (item.Port.Length == 0)
            {
                problems.Add(Error(InissProblemCode.DeviceNoLine, item.Name, protocols[0]));
                continue;
            }

            var line = Find(lines, item.Port, item.Name, protocols, problems);
            if (line is null || IsMaster(line, item.Name, problems))
                continue;

            var address = UhAddress;
            var channel = 1;
            if (uh)
            {
                if (item.Channel.Length > 0 && !(int.TryParse(item.Channel, NumberStyles.None, CultureInfo.InvariantCulture, out channel) && channel is 1 or 2))
                {
                    problems.Add(Error(InissProblemCode.AudioChannel, item.Name, item.Channel));
                    continue;
                }
            }
            else
            {
                if (item.Address is < 1 or > 255)
                {
                    problems.Add(Error(InissProblemCode.DeviceAddress, item.Name, Num(item.Address)));
                    continue;
                }

                address = item.Address;
            }

            Entry(found, line, address).Audio.Add(new StationAudioDto(item.Name, item.Station, channel));
        }

        var controllers = found.GroupBy(f => f.Key.Line, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<InissControllerDto>)g.OrderBy(f => f.Key.Address)
                .Select(f => new InissControllerDto(f.Key.Address, f.Value.Protocol, Name(f.Value.Lights, f.Value.Audio), f.Value.Lights, f.Value.Audio))
                .ToList(), StringComparer.Ordinal);
        return new Result(controllers, problems);
    }

    /// <summary>Linka podla kluca; server porovnava kluc presne a berie prvu.</summary>
    private static CommLine? Find(IReadOnlyList<CommLine> lines, string key, string name, string[] protocols, List<InissProblemDto> problems)
    {
        var line = lines.FirstOrDefault(l => l.Key == key);
        if (line is null)
        {
            problems.Add(Error(InissProblemCode.DeviceUnknownLine, name, key));
            return null;
        }

        if (!protocols.Contains(line.Protocol, StringComparer.Ordinal))
        {
            problems.Add(Error(InissProblemCode.DeviceWrongProtocol, name, key, line.Protocol));
            return null;
        }

        return line;
    }

    /// <summary>Jedina linka s vhodnym protokolom - svetlo bez <c>lin</c>.</summary>
    private static CommLine? Only(IReadOnlyList<CommLine> lines, string name, string[] protocols, List<InissProblemDto> problems)
    {
        var candidates = lines.Where(l => protocols.Contains(l.Protocol, StringComparer.Ordinal)).ToList();
        if (candidates.Count == 1)
            return candidates[0];
        problems.Add(Error(candidates.Count == 0 ? InissProblemCode.DeviceNoLine : InissProblemCode.DeviceLineAmbiguous, name, protocols[0]));
        return null;
    }

    private static bool IsMaster(CommLine line, string name, List<InissProblemDto> problems)
    {
        if (line.Protocol != MasterLight)
            return false;
        problems.Add(new InissProblemDto(InissProblemSeverity.Warning, InissProblemCode.MasterLight, [name, line.Key]));
        return true;
    }

    private static (string Protocol, List<StationLightDto> Lights, List<StationAudioDto> Audio) Entry(
        Dictionary<(string, int), (string Protocol, List<StationLightDto> Lights, List<StationAudioDto> Audio)> found, CommLine line, int address)
    {
        if (!found.TryGetValue((line.Key, address), out var entry))
            found[(line.Key, address)] = entry = (line.Protocol, [], []);
        return entry;
    }

    /// <summary>Nazov ustredne pre obsluhu: svetla a potom audio linky.</summary>
    private static string Name(List<StationLightDto> lights, List<StationAudioDto> audio) =>
        string.Join(", ", lights.Select(l => l.Name).Concat(audio.Select(a => a.Name)).Where(n => n.Length > 0).Distinct(StringComparer.Ordinal));

    private static InissProblemDto Error(InissProblemCode code, params string[] args) => new(InissProblemSeverity.Error, code, args);

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
}
