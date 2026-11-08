using System.Buffers.Binary;
using System.Text;

namespace TableSimulator.Application.Devices.Sounds;

/// <summary>Uprava hlavicky WAV z banky INISSu pred prehratim v prehliadaci.</summary>
public static class WavHeader
{
    /// <summary>
    /// Posledny blok nahravok INISSu byva skrateny (hlavicka udava viac dat, nez je v subore) - velkost bloku <c>data</c>
    /// a celeho RIFF sa zmensi na skutocnu, aby nahravku prehrali vsetky prehliadace. Iny nez WAV obsah ostane.
    /// </summary>
    public static void ClampData(Span<byte> wav)
    {
        if (wav.Length < 12 || !wav[..4].SequenceEqual("RIFF"u8) || !wav[8..12].SequenceEqual("WAVE"u8))
            return;

        var position = 12;
        while (wav.Length - position >= 8)
        {
            var id = Encoding.ASCII.GetString(wav.Slice(position, 4));
            var size = BinaryPrimitives.ReadUInt32LittleEndian(wav.Slice(position + 4, 4));
            var available = (uint)(wav.Length - position - 8);
            if (id == "data")
            {
                if (size > available)
                    BinaryPrimitives.WriteUInt32LittleEndian(wav.Slice(position + 4, 4), available);
                break;
            }

            if (size >= available)
                break;
            position += 8 + (int)size + (int)(size & 1);
        }

        var riff = (uint)(wav.Length - 8);
        if (BinaryPrimitives.ReadUInt32LittleEndian(wav.Slice(4, 4)) > riff)
            BinaryPrimitives.WriteUInt32LittleEndian(wav.Slice(4, 4), riff);
    }
}
