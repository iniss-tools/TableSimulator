using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Scenarios;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;
using TableSimulator.Core.Boards.Chars;
using TableSimulator.Core.Boards.Flaps;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Mapping;

/// <summary>Prevod poziadaviek API na nastavenie simulatora.</summary>
public static partial class RequestMapper
{
    /// <summary>Nastavenie linky s cislom z adresy poziadavky.</summary>
    public static LineConfig ToConfig(SetLineRequest request, int number)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new LineConfig(number, request.TableClass, request.Endpoint)
        {
            Name = request.Name,
            Mode = request.Mode == Contracts.Lines.LineMode.Monitor ? Core.Lines.LineMode.Monitor : Core.Lines.LineMode.Display,
            AutoLearn = request.AutoLearn,
            Manufacturer = request.Manufacturer,
            Mirror = string.IsNullOrWhiteSpace(request.Mirror) ? null : request.Mirror.Trim()
        };
    }

    /// <summary>Nastavenie tabule s linkou a adresou z adresy poziadavky.</summary>
    public static BoardConfig ToConfig(SetBoardRequest request, int line, int address)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new BoardConfig(line, address, request.Manufacturer)
        {
            Name = request.Name,
            Profile = ToProfile(request.Profile),
            Design = string.IsNullOrWhiteSpace(request.Design) ? null : request.Design,
            Fault = ToFault(request.Fault),
            SecondAddress = request.SecondAddress
        };
    }

    public static BoardProfile ToProfile(BoardProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new BoardProfile
        {
            Width = profile.Width,
            Rows = profile.Rows,
            RowHeight = profile.RowHeight,
            FontFile = string.IsNullOrWhiteSpace(profile.FontFile) ? null : profile.FontFile,
            FontMap = profile.FontMap,
            CharFonts = profile.CharFonts is { Count: > 0 } fonts
                ? fonts.ToDictionary(f => f.Key.Trim(), f => new CharFont(f.Value.Font?.Trim() ?? "", f.Value.Bold), StringComparer.Ordinal)
                : null,
            AlternationMs = profile.AlternationMs,
            BlinkOnMs = profile.BlinkOnMs,
            BlinkOffMs = profile.BlinkOffMs,
            ScrollSpeed = profile.ScrollSpeed,
            Monochrome = profile.Monochrome,
            SwapRedGreen = profile.SwapRedGreen,
            Header = profile.Header is { } h
                ? new BoardHeader
                {
                    Height = h.Height,
                    Texts = h.Texts.Select(t => new HeaderText(t.X, t.Y, t.Height, t.Text)).ToArray(),
                    Clock = h.Clock is { } c ? new HeaderClock(c.X, c.Y, c.Height) : null
                }
                : null,
            Face = string.IsNullOrWhiteSpace(profile.Face) ? null : profile.Face.Trim(),
            FaceValues = FaceValues(profile.FaceValues),
            MovingFont = profile.MovingFont,
            AutoSize = profile.AutoSize,
            RotationMs = profile.RotationMs,
            Flaps = profile.Flaps is { } f ? ToFlaps(f) : null,
            AkSignal = profile.AkSignal is { } a ? ToAkSignal(a) : null
        };
    }

    /// <summary>Ustredna AK Signal z profilu tabule.</summary>
    public static AkSignalProfile ToAkSignal(AkSignalProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return ToAkSignal(profile.Lights ?? [], profile.Audio ?? []) with
        {
            Sensor = ToSensor(profile.Sensor),
            Conditions = ToConditions(profile.Conditions),
            SoundSeconds = profile.SoundSeconds,
            SyncSeconds = profile.SyncSeconds,
            SilenceSeconds = profile.SilenceSeconds
        };
    }

    /// <summary>Snimac, stavy a casovanie ustredne zo ziadosti; svetla a audio linky ostanu.</summary>
    public static AkSignalProfile WithSettings(AkSignalProfile profile, AkSignalSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(request);
        return profile with
        {
            Sensor = ToSensor(request.Sensor),
            Conditions = ToConditions(request.Conditions),
            SoundSeconds = request.SoundSeconds ?? profile.SoundSeconds,
            SyncSeconds = request.SyncSeconds ?? profile.SyncSeconds,
            SilenceSeconds = request.SilenceSeconds ?? profile.SilenceSeconds
        };
    }

    /// <summary>Prekrytie ustredne z kroku scenara.</summary>
    public static AkSignalOverride ToOverride(ScenarioDeviceDto device)
    {
        ArgumentNullException.ThrowIfNull(device);
        return new AkSignalOverride(ToConditions(device.Conditions), device.Sensor is { } mode ? ToMode(mode) : null);
    }

    private static TwilightSensor ToSensor(TwilightSensorDto? sensor) => sensor is null
        ? new TwilightSensor()
        : new TwilightSensor { Mode = ToMode(sensor.Mode), OffsetMinutes = sensor.OffsetMinutes };

    private static Core.Boards.AkSignal.TwilightMode ToMode(Contracts.Boards.TwilightMode mode) => mode switch
    {
        Contracts.Boards.TwilightMode.Dark => Core.Boards.AkSignal.TwilightMode.Dark,
        Contracts.Boards.TwilightMode.Light => Core.Boards.AkSignal.TwilightMode.Light,
        _ => Core.Boards.AkSignal.TwilightMode.Sun
    };

    private static AkSignalConditions ToConditions(AkSignalConditionsDto? c) => c is null
        ? new AkSignalConditions()
        : new AkSignalConditions
        {
            LightsFault = c.LightsFault, HighPriority = c.HighPriority, OutputPowerError = c.OutputPowerError, AmpSupplyMissing = c.AmpSupplyMissing,
            AmpCommError = c.AmpCommError, BankNotSynced = c.BankNotSynced
        };

    /// <summary>Hodnoty premennych cela bez prazdnych; null = ziadne.</summary>
    private static Dictionary<string, string>? FaceValues(IReadOnlyDictionary<string, string>? values)
    {
        var kept = values?.Where(v => !string.IsNullOrWhiteSpace(v.Key) && !string.IsNullOrEmpty(v.Value))
            .GroupBy(v => v.Key.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.Ordinal);
        return kept is { Count: > 0 } ? kept : null;
    }

    /// <summary>Svetla a audio linky ustredne AK Signal z dat stanice; snimac a stavy su predvolene.</summary>
    public static AkSignalProfile ToAkSignal(IEnumerable<StationLightDto> lights, IEnumerable<StationAudioDto> audio) => new()
    {
        Lights = lights.Select(l => new LightCircuit(l.Name, l.Station)
        {
            BeforeArrival = l.BeforeArrival, BeforeDeparture = l.BeforeDeparture, AfterDeparture = l.AfterDeparture, AfterArrival = l.AfterArrival,
            MinOff = l.MinOff
        }).ToList(),
        Audio = audio.Select(a => new AudioCircuit(a.Name, a.Station) { Channel = a.Channel }).ToList()
    };

    public static FlapProfile ToFlaps(FlapProfileDto flaps)
    {
        ArgumentNullException.ThrowIfNull(flaps);
        return new FlapProfile
        {
            LinesPerRecord = flaps.LinesPerRecord,
            Modules = flaps.Modules.Select(m => new FlapModule(m.Line, m.Position, m.Span, m.List)).ToArray(),
            Lists = Copy(flaps.Lists),
            Colors = Copy(flaps.Colors)
        };
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> Copy(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> lists) =>
        lists.ToDictionary(l => l.Key, IReadOnlyDictionary<string, string> (l) => l.Value.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);

    public static BoardFault ToFault(BoardFaultDto fault)
    {
        ArgumentNullException.ThrowIfNull(fault);
        return new BoardFault(fault.Kind switch
        {
            Contracts.Boards.FaultKind.None => Core.Boards.FaultKind.None,
            Contracts.Boards.FaultKind.NoReply => Core.Boards.FaultKind.NoReply,
            Contracts.Boards.FaultKind.Nak => Core.Boards.FaultKind.Nak,
            Contracts.Boards.FaultKind.Delay => Core.Boards.FaultKind.Delay,
            Contracts.Boards.FaultKind.Garbage => Core.Boards.FaultKind.Garbage,
            Contracts.Boards.FaultKind.Status => Core.Boards.FaultKind.Status,
            _ => throw new ArgumentOutOfRangeException(nameof(fault), fault.Kind, null)
        }, fault.DelayMs, fault.Status);
    }

    public static StationImport ToImport(StationImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new StationImport
        {
            Lines = request.Lines.Select(l => new StationLine(l.Number, l.TableClass, l.Port)
            {
                Name = l.Name, Transport = l.Transport == StationLineTransport.Udp ? LineTransport.Udp : LineTransport.Tcp
            }).ToList(),
            Boards = request.Boards.Select(b => new StationBoard(b.Line, b.Address, b.Manufacturer)
            {
                Name = b.Name, Flaps = b.Flaps is { } f ? ToFlaps(f) : null, FlapRecords = b.FlapRecords,
                Width = b.Face is { Width: > 0 } face && b.Flaps is null ? Math.Max(face.Width, face.Columns.Select(c => c.End).DefaultIfEmpty().Max()) : 0,
                Rows = b.Face is { Width: > 0 } rows && b.Flaps is null ? Math.Max(1, rows.Records) * Math.Max(1, rows.LinesPerRecord) : 0,
                AkSignal = b.Lights is { Count: > 0 } || b.Audio is { Count: > 0 } ? ToAkSignal(b.Lights ?? [], b.Audio ?? []) : null
            }).ToList(),
            FacesForExisting = request.FacesForExisting
        };
    }
}
