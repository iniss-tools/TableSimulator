using System.Collections.Concurrent;
using ToolsCore.Iniss.Entities;
using ToolsCore.Iniss.Grafikon;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Application.Devices;

/// <summary>
/// Dlzky nahravok banky INISS 3.39 podla odkazu <c>jazyk/skupina/zvuk</c>, ako ho INISS posiela vzdialenemu INISSu
/// v <c>RWAV</c>: kluc jazyka z FYZBANK.DAT, kluc skupiny a zvuku z FYZZVUK.DAT (bez ohladu na velkost pismen, plati
/// prva zhoda). Dlzka je z FYZZVUK.DAT; ak je 0, z hlavicky suboru nahravky. Banka sa nacita pri prvom pouziti.
/// </summary>
/// <param name="rawBankDir">priecinok banky (<c>RAWBANK</c>)</param>
public sealed class Iniss339SoundLengths(string rawBankDir)
{
    private readonly string _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rawBankDir)) + Path.DirectorySeparatorChar;
    private readonly Lazy<Dictionary<string, FyzSound>> _sounds = new(() => Load(rawBankDir));
    private readonly ConcurrentDictionary<string, TimeSpan?> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Banka INISS 3.39 vedla programu; null = nie je (ina instalacia alebo chyba FYZBANK.DAT).</summary>
    public static string? BankOf(string program)
    {
        ArgumentNullException.ThrowIfNull(program);
        var folder = Path.GetDirectoryName(Path.GetFullPath(program));
        if (folder is null)
            return null;
        var bank = Path.Combine(folder, GvdFileConsts.DirRawbank);
        return File.Exists(RawBankParser.FyzBankFile(bank + Path.DirectorySeparatorChar)) ? bank : null;
    }

    /// <summary>Dlzka nahravky; null = banka ju nepozna alebo sa neda precitat.</summary>
    public TimeSpan? Length(string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return _cache.GetOrAdd(reference, Read);
    }

    private TimeSpan? Read(string reference)
    {
        if (!_sounds.Value.TryGetValue(reference.Trim(), out var sound))
            return null;
        if (sound.Duration > 0)
            return TimeSpan.FromMilliseconds(sound.Duration);

        // len subory v banke - cesta zo zoznamu nesmie viest mimo nej
        try
        {
            var file = Path.GetFullPath(sound.GetAbsPath(_root));
            if (!file.StartsWith(_root, StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
                return null;
            using var stream = File.OpenRead(file);
            return EwaCodec.Duration(stream);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Zvuky banky podla odkazu; chybna alebo chybajuca banka = ziadne.</summary>
    private static Dictionary<string, FyzSound> Load(string rawBankDir)
    {
        var sounds = new Dictionary<string, FyzSound>(StringComparer.OrdinalIgnoreCase);
        var bank = Path.TrimEndingDirectorySeparator(rawBankDir) + Path.DirectorySeparatorChar;
        try
        {
            foreach (var language in RawBankParser.ReadFyzBankFile(bank, out _))
            {
                List<FyzSound> list;
                try
                {
                    list = RawBankParser.ReadFyzZvukFile(bank, language);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var sound in list)
                    sounds.TryAdd($"{language.Key}/{sound.Group.Key}/{sound.Key}", sound);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // banka sa neda precitat - dlzky budu predvolene
        }

        return sounds;
    }
}
