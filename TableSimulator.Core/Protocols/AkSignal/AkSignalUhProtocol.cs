using System.Globalization;
using System.Text;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;

namespace TableSimulator.Core.Protocols.AkSignal;

/// <summary>
/// Ustredna AK Signal UH s protokolom AKSIGNALUH (TableClass 132 simulatora): na linke jedna ustredna bez adresy (v
/// simulatore adresa 0) so svetlom a dvoma kanalmi zvuku, ktore hraju zvuky samy. Dlzka hlasenia je sucet dlzok zvukov
/// z banky servera (<see cref="Simulation.SimEnvironment.SoundLength" />), inak <see cref="AkSignalProfile.SoundSeconds" />
/// na zvuk. Dotaz <c>&lt;aud&gt;</c> bez kanala hlasi hranie ktorehokolvek kanala - to je predpoklad, server ma na
/// linke zvycajne jeden kanal. Viz docs/iniss2/protokoly/aksignal.mdx.
/// </summary>
internal sealed class AkSignalUhProtocol : ILineProtocol
{
    /// <summary>Kluc vyrobcu ustredne v simulatore.</summary>
    public const string Manufacturer = "AKSIGNALUH";

    /// <summary>Adresa ustredne na linke - protokol adresu nema.</summary>
    public const int Address = 0;

    private static readonly TimeSpan ReceiveGap = TimeSpan.FromMilliseconds(500);

    private static readonly (int Bit, string Key)[] AudioKeys =
        [(0x01, "run"), (0x02, "hiprt"), (0x04, "cancl"), (0x08, "pwr"), (0x10, "sup"), (0x20, "comerr"), (0x40, "sync")];

    private static readonly (int Bit, string Key)[] LightKeys = [(0x01, "lit"), (0x02, "man"), (0x04, "dark"), (0x08, "ok"), (0x10, "on")];

    private readonly AkSignalUhFramer _framer = new();
    private DateTimeOffset _lastData;

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay(string manufacturer) => new AkSignalDisplay();

    /// <summary>
    /// Pri poruche ustredna odpovie nedokoncenou spravou (nezmysel), odpovedou bez stavu (NAK) alebo zvolenym stavom -
    /// bity ako kluce svetla (<c>1 lit, 2 man, 4 dark, 8 ok, 16 on</c>) alebo zvuku (<c>1 run … 64 sync</c>).
    /// </summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal)
    {
        var command = CommandOf(normal);
        switch (fault.Kind)
        {
            case FaultKind.Garbage:
                return (Encoding.UTF8.GetBytes("<#" + command), new FrameInfo("garbage"));
            case FaultKind.Status:
                var keys = (command == AkSignalUhFrame.Light ? LightKeys : AudioKeys).Where(k => (fault.Status & k.Bit) != 0).Select(k => k.Key).ToList();
                var state = State(command, keys);
                return (AkSignalUhFrame.Answer(command, state), StateInfo(state));
            default:
                return (Encoding.UTF8.GetBytes("<#" + command + "|err>"), new FrameInfo("aksignal.corrupt"));
        }
    }

    public void Receive(ReadOnlySpan<byte> data, ILineContext context)
    {
        var discarded = _framer.DiscardedBytes;
        if (_framer.Pending > 0 && context.Now - _lastData > ReceiveGap)
            _framer.DropPending();
        _lastData = context.Now;

        foreach (var frame in _framer.Push(data))
            Handle(frame, context);

        if (_framer.DiscardedBytes != discarded)
        {
            context.Line.Stats.Discarded += _framer.DiscardedBytes - discarded;
            context.LogReceived([], null, FrameStatus.Noise, new FrameInfo("noise", Args(("count", Num(_framer.DiscardedBytes - discarded)))));
        }
    }

    private static void Handle(AkSignalUhFrame frame, ILineContext context)
    {
        context.Line.Stats.Frames++;
        var target = context.FindBoard(Address);
        if (target?.Display is not AkSignalDisplay display)
        {
            context.LogReceived(frame.Raw, Address, FrameStatus.NoBoard, Describe(frame));
            return;
        }

        target.Stats.Frames++;
        target.Stats.LastFrameAt = context.Now;

        var now = context.Now;
        var profile = target.EffectiveProfile;
        var changed = display.Advance(now, profile, context.Environment.Location);
        string? state = null;
        switch (frame.Command)
        {
            case AkSignalUhFrame.Audio:
            {
                changed |= display.QueryAudio(now);
                int? channel = int.TryParse(frame.Value("ch"), NumberStyles.None, CultureInfo.InvariantCulture, out var ch) ? ch : null;
                var action = frame.Parameters.FirstOrDefault(p => !p.StartsWith("ch=", StringComparison.Ordinal));
                if (channel is { } c && action == AkSignalUhFrame.Stop)
                    changed |= display.Stop(c, now);
                else if (channel is { } p && !string.IsNullOrEmpty(action))
                    changed |= display.Play(p, Sounds(action), Duration(Sounds(action), profile, context), now, profile);
                state = State(frame.Command, display.AudioKeys(profile, channel, now));
                break;
            }
            case AkSignalUhFrame.Sync:
                display.QueryAudio(now);
                display.Sync(Banks(frame), now, profile);
                changed = true;
                state = State(frame.Command, display.AudioKeys(profile, null, now));
                break;
            case AkSignalUhFrame.Light:
                changed |= display.QueryLight(now);
                if (frame.Value("man") is { } manual)
                    changed |= display.SetManual(manual == "1", now);
                if (frame.Value("lit") is { } lit)
                    changed |= display.SetAuto(lit == "1", now);
                state = State(frame.Command, display.LightKeys(profile));
                break;
        }

        if (changed)
            context.Changed(target);
        context.LogReceived(frame.Raw, Address, FrameStatus.Ok, Describe(frame));
        if (state is not null)
            context.Reply(target, AkSignalUhFrame.Answer(frame.Command, state), StateInfo(state));
    }

    /// <summary>Stav odpovede: kluce oddelene ciarkou; zvuk bez klucov ako <c>0</c>, svetlo prazdne (<c>0</c> server pri svetle odmietne).</summary>
    private static string State(string command, IEnumerable<string> keys)
    {
        var text = string.Join(",", keys);
        return text.Length == 0 && command != AkSignalUhFrame.Light ? "0" : text;
    }

    private static List<string> Sounds(string parameter) => parameter.Split('*', StringSplitOptions.RemoveEmptyEntries).ToList();

    private static TimeSpan Duration(IReadOnlyList<string> sounds, BoardProfile profile, ILineContext context)
    {
        var fallback = TimeSpan.FromSeconds(Math.Max(0, (profile.AkSignal ?? AkSignalProfile.Default).SoundSeconds));
        var total = TimeSpan.Zero;
        foreach (var sound in sounds)
            total += context.Environment.SoundLength?.Invoke(sound) ?? fallback;
        return total;
    }

    private static Dictionary<string, string> Banks(AkSignalUhFrame frame)
    {
        var banks = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in frame.Parameters)
        {
            var equals = parameter.IndexOf('=', StringComparison.Ordinal);
            if (equals > 0)
                banks[parameter[..equals]] = parameter[(equals + 1)..];
        }

        return banks;
    }

    private static string CommandOf(byte[] answer)
    {
        var text = Encoding.UTF8.GetString(answer);
        var start = text.StartsWith("<#", StringComparison.Ordinal) ? 2 : 0;
        var end = text.IndexOfAny(['|', '>'], start);
        return end > start ? text[start..end] : text[start..];
    }

    /// <summary>Vyznam prikazu - kluce svetla a zvuku zdiela s protokolom AKSIGNAL.</summary>
    private static FrameInfo Describe(AkSignalUhFrame frame)
    {
        switch (frame.Command)
        {
            case AkSignalUhFrame.Audio:
            {
                var channel = frame.Value("ch") ?? "";
                var action = frame.Parameters.FirstOrDefault(p => !p.StartsWith("ch=", StringComparison.Ordinal));
                if (string.IsNullOrEmpty(action))
                    return new FrameInfo("aksignal.getAudio");
                return action == AkSignalUhFrame.Stop
                    ? new FrameInfo("aksignal.stop", Args(("channel", channel)))
                    : new FrameInfo("aksignal.play", Args(("channel", channel), ("sounds", action.Replace("*", ", ", StringComparison.Ordinal))));
            }
            case AkSignalUhFrame.Sync:
                return new FrameInfo("aksignal.sync", Args(("banks", string.Join(", ", frame.Parameters))));
            case AkSignalUhFrame.Light:
                if (frame.Value("man") is { } manual)
                    return new FrameInfo(manual == "1" ? "aksignal.setManual" : "aksignal.clearManual");
                if (frame.Value("lit") is { } lit)
                    return new FrameInfo(lit == "1" ? "aksignal.setLight" : "aksignal.clearLight");
                return new FrameInfo("aksignal.getLight");
            default:
                return new FrameInfo("aksignal.unknown", Args(("cmd", frame.Command)));
        }
    }

    private static FrameInfo StateInfo(string state) => new("aksignal.keys", Args(("state", state.Length == 0 ? "-" : state)));

    private static string Num(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);
}
