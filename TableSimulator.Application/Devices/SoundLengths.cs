using System.Collections.Concurrent;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Application.Devices;

/// <summary>
/// Dlzky zvukov banky servera INISS2 podla mena, ako ho server posiela ustredni AK Signal UH (<c>CZ/vlak/1234</c> =
/// subor <c>RawBank\CZ\vlak\1234.EWA</c>). Dlzka sa zisti z hlavicky WAV pri prvom pouziti a zapamata sa.
/// </summary>
/// <param name="rawBankDir">priecinok banky zvukov servera</param>
public sealed class SoundLengths(string rawBankDir)
{
    private const string Extension = ".EWA";

    private readonly string _root = Path.GetFullPath(rawBankDir);
    private readonly ConcurrentDictionary<string, TimeSpan?> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Priecinok banky zvukov.</summary>
    public string Root => _root;

    /// <summary>Dlzka zvuku; null = subor nie je v banke alebo sa neda precitat.</summary>
    public TimeSpan? Length(string sound)
    {
        ArgumentNullException.ThrowIfNull(sound);
        return _cache.GetOrAdd(sound, Read);
    }

    private TimeSpan? Read(string sound)
    {
        var relative = sound.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (!relative.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            relative += Extension;

        // len subory v banke - meno od servera nesmie viest mimo nej
        string file;
        try
        {
            file = Path.GetFullPath(Path.Combine(_root, relative));
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        if (!file.StartsWith(Path.TrimEndingDirectorySeparator(_root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
            return null;

        try
        {
            using var stream = File.OpenRead(file);
            return EwaCodec.Duration(stream);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
        {
            return null;
        }
    }
}
