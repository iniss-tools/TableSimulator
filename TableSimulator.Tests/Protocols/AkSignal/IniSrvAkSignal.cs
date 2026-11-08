using System.Text;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Tests.Protocols.AkSignal;

/// <summary>
/// Strana servera INISS2 pri riadeni svetla a stave zvuku tak, ako ju robi IniSrv (rozbor dekompilatu ovladacov
/// DriverAKSignalLight, DriverAKSignalUHLight, DriverAKSignalAudio a DriverAKSignalUH): kontrola odpovede, prikazy
/// automatiky a stav svetla pre klienta. Testy ju pouzivaju, aby odpovede simulatora vyhodnotil server, nie simulator.
/// </summary>
internal sealed class IniSrvAkSignal(Simulator sim, int line, int address, bool uh)
{
    /// <summary>Rezim svetla: vypnute.</summary>
    public const int ModeOff = 0;

    /// <summary>Rezim svetla: automatika.</summary>
    public const int ModeAuto = 1;

    /// <summary>Rezim svetla: rucne.</summary>
    public const int ModeForce = 2;

    /// <summary>Stav svetla: svieti.</summary>
    public const int LightOn = 0x10;

    /// <summary>Stav svetla: je tma.</summary>
    public const int Twilight = 0x20;

    /// <summary>Stav svetla: porucha svetiel.</summary>
    public const int Failure = 0x40;

    /// <summary>Stav svetla: ustredna nekomunikuje.</summary>
    public const int NotConnected = int.MinValue;

    private static readonly Dictionary<string, int> UhLight = new(StringComparer.Ordinal) { ["lit"] = 1, ["man"] = 2, ["dark"] = 4, ["ok"] = 8, ["on"] = 0x10 };

    private static readonly Dictionary<string, int> UhAudio = new(StringComparer.Ordinal)
        { ["run"] = 1, ["hiprt"] = 2, ["cancl"] = 4, ["pwr"] = 8, ["sup"] = 0x10, ["comerr"] = 0x20, ["sync"] = 0x40 };

    /// <summary>Vlaky potrebuju svetlo (AutoCondition servera).</summary>
    public bool RequireAuto { get; set; }

    /// <summary>Stav svetla po poslednom kole (LIGHTSTATUS servera).</summary>
    public int Status { get; private set; }

    /// <summary>Kolko prikazov server v poslednom kole poslal.</summary>
    public int Sent { get; private set; }

    /// <summary>Hlasky CHYBA_STAVU v poslednom kole.</summary>
    public int StateErrors { get; private set; }

    /// <summary>
    /// Jedno kolo CheckStateAndUpdateMode: dotaz alebo rucny prikaz (<paramref name="setMode" /> 2 = rucne, 1 = zrusit),
    /// pri nesulade automatiky jeden prikaz zapnutia alebo vypnutia a stav z poslednej odpovede.
    /// </summary>
    public int CheckLight(int? setMode = null)
    {
        Sent = 0;
        StateErrors = 0;
        var command = setMode switch
        {
            ModeForce => uh ? "man=1" : "23",
            ModeAuto => uh ? "man=0" : "24",
            _ => uh ? "" : "20"
        };
        var corrected = false;
        while (true)
        {
            int? state = null;
            for (var attempt = 0; attempt < 5 && state is null; attempt++)
                state = uh ? SendUhLight(command) : SendLight(Convert.ToByte(command, 16));
            if (state is not { } s)
            {
                Status = NotConnected;
                return Status;
            }

            var mode = (s & 3) switch { 0 => ModeOff, 1 => ModeAuto, _ => ModeForce };
            if (RequireAuto ^ ((s & 1) != 0) && !corrected)
            {
                corrected = true;
                command = uh ? RequireAuto ? "lit=1" : "lit=0" : RequireAuto ? "22" : "21";
                continue;
            }

            Status = mode;
            if ((s & 2) != 0 || ((s & 1) != 0 && (s & 4) != 0))
                Status |= LightOn;
            if ((s & 4) != 0)
                Status |= Twilight;
            if ((s & 8) == 0)
                Status |= Failure;
            return Status;
        }
    }

    /// <summary>Stav zvuku AKSIGNAL (bajt odpovede) alebo AKSIGNALUH (bity klucov); null = bez platnej odpovede.</summary>
    public int? Audio(string? uhParameters = null) =>
        uh ? SendUh("aud", uhParameters, UhAudio) : SendLight(0x10);

    /// <summary>Prikaz AKSIGNAL; stav z odpovede alebo null, ak ju server odmietne.</summary>
    public int? SendLight(byte command, int value = 0xFFFF)
    {
        Sent++;
        byte[] packet = command == 0x12
            ? [(byte)address, 6, command, (byte)(value & 0xFF), (byte)(value >> 8), 0]
            : [(byte)address, 4, command, 0];
        packet[^1] = (byte)packet[..^1].Sum(b => b);
        var answer = sim.Receive(line, packet).SelectMany(r => r.Bytes).ToArray();

        // CheckPacket servera
        if (answer.Length != 5 || answer[1] != 5)
            return null;
        if (answer[4] != (byte)answer[..4].Sum(b => b))
            return null;
        if (answer[0] != 0 || answer[2] != command)
            return null;
        return answer[3];
    }

    /// <summary>Prikaz AKSIGNALUH; stav podla slovnika klucov alebo null, ak server odpoved odmietne.</summary>
    public int? SendUh(string command, string? parameters, IReadOnlyDictionary<string, int> keys)
    {
        Sent++;
        var packet = Encoding.UTF8.GetBytes("<" + command + (string.IsNullOrEmpty(parameters) ? "" : "|" + parameters) + ">");
        var text = Encoding.UTF8.GetString(sim.Receive(line, packet).SelectMany(r => r.Bytes).ToArray());

        // CheckPacket servera
        if (text.Length < 4 || !text.StartsWith("<#", StringComparison.Ordinal) || !text.EndsWith('>'))
            return null;
        var parts = text[2..^1].Split('|');
        if (parts[0] != command)
            return null;
        var state = "";
        if (parts.Length > 1)
        {
            if (!parts[1].StartsWith("st=", StringComparison.Ordinal))
                return null;
            state = parts[1][3..];
        }

        // ConvertState: zvuk berie 0 ako prazdny stav, svetlo nie; neznamy kluc = CHYBA_STAVU a stav 0
        var result = 0;
        if (state.Length > 0 && !(keys == UhAudio && state == "0"))
        {
            foreach (var key in state.Split(','))
            {
                if (!keys.TryGetValue(key, out var bit))
                {
                    StateErrors++;
                    return 0;
                }

                result |= bit;
            }
        }

        return result;
    }

    private int? SendUhLight(string parameters) => SendUh("light", parameters, UhLight);
}
