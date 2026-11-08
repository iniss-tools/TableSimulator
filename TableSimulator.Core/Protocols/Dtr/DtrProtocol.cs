using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Protocols.Dtr;

/// <summary>
/// Spinanie zosilnovaca signalom DTR (INISS 3.39, <c>Zvuky\AmplifierPort</c>): INISS na seriovom porte pred hlasenim
/// zapne DTR a po nom ho vypne, data neposiela. Simulator stav cita na druhom konci paru COM portov (null-modem,
/// napr. com0com) ako DSR alebo DCD - linku obsluhuje <see cref="Transport.LineHost" />, ktory vola
/// <see cref="Simulator.SetModemSignal" />. Zopnutie ukazuje zariadenie <see cref="Manufacturer" /> na adrese
/// <see cref="DeviceAddress" /> s obsahom <see cref="AmplifierDisplay" />. Trieda linky je len simulatora.
/// </summary>
internal sealed class DtrProtocol : ILineProtocol
{
    /// <summary>Trieda linky "spinanie DTR" - INISS taku triedu nema.</summary>
    public const int TableClass = 900;

    /// <summary>Druh zariadenia.</summary>
    public const string Manufacturer = "DTR";

    /// <summary>Adresa zariadenia na linke.</summary>
    public const int DeviceAddress = 0;

    public void Reset()
    {
    }

    /// <summary>INISS na port zosilnovaca data neposiela - co pride, je sum.</summary>
    public void Receive(ReadOnlySpan<byte> data, ILineContext context)
    {
        if (data.Length == 0)
            return;
        context.Line.Stats.Discarded += data.Length;
        context.LogReceived(data.ToArray(), null, FrameStatus.Noise,
            new FrameInfo("noise", new Dictionary<string, string> { ["count"] = data.Length.ToString(CultureInfo.InvariantCulture) }));
    }

    public BoardDisplay CreateDisplay(string manufacturer) => new AmplifierDisplay();

    /// <summary>Zosilnovac na DTR neodpoveda - poruchy nemaju co zmenit.</summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal) => (normal, new FrameInfo("dtr"));
}
