using TableSimulator.Application.Settings;
using TableSimulator.Application.Station;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Devices;

/// <summary>
/// Zariadenia, ktore sa menia s casom (ustredne AK Signal: snimac tmy, koniec hlasenia, ticho servera; rozhlas
/// vzdialeneho INISSu): okolie simulatora podla nastavenia - poloha pre slnko a banka zvukov z programu INISSu (servera
/// INISS2 alebo INISSu 3.39) - a posun v case. Server ho vola kazdu sekundu.
/// </summary>
public sealed class DeviceClock(Simulator simulator, ISettingsStore settings)
{
    private readonly Lock _lock = new();
    private string? _program;
    private Core.Boards.AkSignal.GeoLocation? _location;

    /// <summary>Nastavi okolie podla nastavenia, ak sa program INISSu zmenil, a posunie zariadenia v case.</summary>
    public void Tick()
    {
        UpdateEnvironment();
        simulator.Tick();
    }

    /// <summary>
    /// Dlzky zvukov banky programu INISSu z nastavenia: servera INISS2 (subory podla mena) alebo INISSu 3.39 (kluce
    /// banky); null = program nie je nastaveny alebo banku nema.
    /// </summary>
    public static Func<string, TimeSpan?>? SoundLengthOf(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (InissStations.Iniss2Of(settings) is { } installation)
            return Directory.Exists(installation.RawBankDir) ? new SoundLengths(installation.RawBankDir).Length : null;
        var program = settings.InissProgram.Trim();
        return program.Length > 0 && File.Exists(program) && Iniss339SoundLengths.BankOf(program) is { } bank ? new Iniss339SoundLengths(bank).Length : null;
    }

    private void UpdateEnvironment()
    {
        var current = settings.Current;
        bool program, location;
        lock (_lock)
        {
            program = _program != current.InissProgram;
            location = _location != current.Location;
            _program = current.InissProgram;
            _location = current.Location;
        }

        if (location)
            simulator.SetEnvironment(simulator.Environment with { Location = current.Location });
        if (!program)
            return;

        simulator.SetEnvironment(simulator.Environment with { SoundLength = SoundLengthOf(current) });
    }
}
