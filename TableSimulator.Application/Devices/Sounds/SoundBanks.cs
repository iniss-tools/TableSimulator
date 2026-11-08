using TableSimulator.Application.Settings;
using TableSimulator.Application.Station;

namespace TableSimulator.Application.Devices.Sounds;

/// <summary>
/// Banka zvukov programu INISSu z nastavenia - spolocna pre dlzky hlaseni (<see cref="DeviceClock" />) a prehratie
/// nahravky v prehliadaci. Nova sa vytvori az pri zmene programu (banka INISSu 3.39 sa nacitava cela).
/// </summary>
public sealed class SoundBanks(ISettingsStore settings)
{
    private readonly Lock _lock = new();
    private string? _program;
    private ISoundBank? _bank;

    /// <summary>Banka programu v nastaveni; null = program nie je nastaveny alebo banku nema.</summary>
    public ISoundBank? Current
    {
        get
        {
            var current = settings.Current;
            lock (_lock)
            {
                if (_program != current.InissProgram)
                {
                    _bank = Of(current);
                    _program = current.InissProgram;
                }

                return _bank;
            }
        }
    }

    /// <summary>
    /// Banka programu INISSu: servera INISS2 (subory podla mena) alebo INISSu 3.39 (kluce banky); null = program nie je
    /// nastaveny alebo banku nema.
    /// </summary>
    public static ISoundBank? Of(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (InissStations.Iniss2Of(settings) is { } installation)
            return Directory.Exists(installation.RawBankDir) ? new SoundLengths(installation.RawBankDir) : null;
        var program = settings.InissProgram.Trim();
        return program.Length > 0 && File.Exists(program) && Iniss339SoundLengths.BankOf(program) is { } bank ? new Iniss339SoundLengths(bank) : null;
    }
}
