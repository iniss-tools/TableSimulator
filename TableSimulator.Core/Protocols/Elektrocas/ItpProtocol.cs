using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Linka cez sietovy prevodnik KTP (TableClass 130): spravy tabul Elektrocas zabalene do paketov ITP.
/// Simulator hra prevodnik aj tabule za nim - paket rozbali, vnutornu spravu spracuje ako
/// <see cref="ElektrocasProtocol" /> a odpoved tabule zabali s tym istym poradovym cislom.
/// INISS na tejto linke caka na odpoved pri kazdom pakete, aj hromadnom - ked tabula neodpovie,
/// prevodnik hlasi chybu "zariadenie neodpovedalo".
/// </summary>
internal sealed class ItpProtocol : ILineProtocol
{
    /// <summary>Kod chyby prevodnika: bez chyby.</summary>
    public const int KtpOk = 0;

    /// <summary>Kod chyby prevodnika: zariadenie neodpovedalo.</summary>
    public const int KtpNoAnswer = 1;

    private readonly ItpFramer _framer = new();
    private readonly ElektrocasProtocol _inner = new();
    private DateTimeOffset _lastData;

    public void Reset()
    {
        _framer.Reset();
        _inner.Reset();
    }

    public BoardDisplay CreateDisplay(string manufacturer) => _inner.CreateDisplay(manufacturer);

    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal) => _inner.FaultReply(fault, normal);

    public void Receive(ReadOnlySpan<byte> data, ILineContext context)
    {
        var discarded = _framer.DiscardedBytes;
        if (_framer.Pending > 0 && context.Now - _lastData > ElektrocasProtocol.ReceiveGap)
            _framer.DropPending();
        _lastData = context.Now;

        foreach (var packet in _framer.Push(data))
            Handle(packet, context);

        if (_framer.DiscardedBytes != discarded)
        {
            context.Line.Stats.Discarded += _framer.DiscardedBytes - discarded;
            context.LogReceived([], null, FrameStatus.Noise,
                new FrameInfo("noise", Args(("count", (_framer.DiscardedBytes - discarded).ToString(CultureInfo.InvariantCulture)))));
        }
    }

    private void Handle(ItpPacket packet, ILineContext context)
    {
        var seq = packet.Sequence.ToString(CultureInfo.InvariantCulture);
        if (!packet.ChecksumOk || !packet.IsCommand || packet.Protocol != ItpPacket.KtpProtocol)
        {
            // prevodnik zly paket zahodi
            context.Line.Stats.Frames++;
            context.Line.Stats.BadFrames++;
            context.LogReceived(packet.Raw, null, packet.ChecksumOk ? FrameStatus.Noise : FrameStatus.BadChecksum,
                new FrameInfo("itp.bad", Args(("seq", seq), ("kind", packet.Kind.ToString("X4", CultureInfo.InvariantCulture)))));
            return;
        }

        // doplnkovy bajt 00 za vnutornou spravou neparnej dlzky
        var inner = packet.Payload;
        if (inner.Length >= 2 && inner[1] >= ElektrocasFrame.MinLength && inner[1] < inner.Length)
            inner = inner[..inner[1]];

        var innerContext = new InnerContext(context, _inner, packet, seq);
        _inner.Reset();
        _inner.Receive(inner, innerContext);
        if (!innerContext.Logged)
            context.LogReceived(packet.Raw, null, FrameStatus.Noise, new FrameInfo("itp.empty", Args(("seq", seq))));

        if (context.Line.Mode == LineMode.Monitor)
            return;

        if (innerContext.Answer is { } answer)
        {
            context.ReplyRaw(answer.Address, Wrap(packet, KtpOk, answer.Bytes), answer.Delay, answer.Status, answer.Info);
        }
        else
        {
            context.ReplyRaw(innerContext.Address, Wrap(packet, KtpNoAnswer, []), TimeSpan.Zero,
                innerContext.Faulted ? FrameStatus.Fault : FrameStatus.Ok, new FrameInfo("itp.noAnswer", Args(("seq", seq))));
        }
    }

    /// <summary>Odpoved prevodnika: jedno slovo hlavicky s kodom chyby, za nim odpoved tabule.</summary>
    private static byte[] Wrap(ItpPacket packet, int error, ReadOnlySpan<byte> answer)
    {
        byte[] payload = [(byte)error, 0, .. answer];
        return ItpPacket.Build(1 << 8 | ItpPacket.KtpProtocol, packet.Sequence, payload);
    }

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);

    /// <summary>
    /// Kontext vnutornej spravy: zaznam ide s celym paketom, odpoved tabule sa zachyti a zabali.
    /// </summary>
    private sealed class InnerContext(ILineContext outer, ILineProtocol inner, ItpPacket packet, string seq) : ILineContext
    {
        public (int? Address, byte[] Bytes, TimeSpan Delay, FrameStatus Status, FrameInfo Info)? Answer { get; private set; }

        /// <summary>Adresa tabule, ktorej sprava patrila.</summary>
        public int? Address { get; private set; }

        /// <summary>Tabula neodpovedala pre predstieranu poruchu.</summary>
        public bool Faulted { get; private set; }

        /// <summary>Vnutorna sprava sa zapisala do zaznamu.</summary>
        public bool Logged { get; private set; }

        public SimLine Line => outer.Line;

        public DateTimeOffset Now => outer.Now;

        public IReadOnlyList<SimBoard> Boards => outer.Boards;

        public SimBoard? FindBoard(int address, string? manufacturer = null) => outer.FindBoard(address, manufacturer);

        public void Changed(SimBoard board) => outer.Changed(board);

        public void LogReceived(byte[] bytes, int? address, FrameStatus status, FrameInfo info)
        {
            Address ??= address;
            Logged = true;
            var args = new Dictionary<string, string>(info.Args ?? new Dictionary<string, string>()) { ["seq"] = seq };
            outer.LogReceived(bytes.Length > 0 ? packet.Raw : bytes, address, status, info with { Args = args });
        }

        public void Reply(SimBoard board, byte[] bytes, FrameInfo info, bool positive = true)
        {
            var reply = BoardFaults.Apply(board, inner, bytes, info, positive);
            if (reply.Bytes == null)
                Faulted = true;
            else
                Answer = (board.Address, reply.Bytes, reply.Delay, reply.Status, reply.Info);
        }

        public void ReplyRaw(int? address, byte[] bytes, TimeSpan delay, FrameStatus status, FrameInfo info, bool log = true) =>
            Answer = (address, bytes, delay, status, info);

        public void Forward(byte[] frame) => outer.Forward(frame);
    }
}
