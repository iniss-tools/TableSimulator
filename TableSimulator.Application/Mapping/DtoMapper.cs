using System.Globalization;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Display;
using TableSimulator.Contracts.Frames;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Monitor;
using TableSimulator.Contracts.Recordings;
using TableSimulator.Contracts.Settings;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Monitor;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Recording;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Mapping;

/// <summary>
/// Prevod stavu simulatora na kontrakty API - stav tabul a liniek sa cita pod zamkom simulatora.
/// </summary>
public static partial class DtoMapper
{
    /// <summary>Vyrobcovia, ktorych tabule simulator zobrazi, podla triedy linky.</summary>
    public static IReadOnlyDictionary<string, string[]> Manufacturers { get; } = Simulator.SupportedClasses.ToDictionary(
        c => c.ToString(CultureInfo.InvariantCulture), c => Simulator.ManufacturersFor(c).ToArray());

    /// <summary>Linka; <paramref name="connectHost" /> je adresa servera v TablePort pre INISS (prazdna = 127.0.0.1).</summary>
    public static LineDto ToDto(SimLine line, string? connectHost)
    {
        ArgumentNullException.ThrowIfNull(line);
        var s = line.Stats;
        return new LineDto(
            line.Number, line.Name, line.TableClass, line.Endpoint.ToString(), ToDto(line.Mode), line.AutoLearn, line.Manufacturer,
            ToDto(line.Status), line.StatusDetail,
            line.Endpoint.ToInissTablePort(line.Number, string.IsNullOrWhiteSpace(connectHost) ? null : connectHost),
            new LineStatsDto(s.BytesIn, s.BytesOut, s.Frames, s.BadFrames, s.Discarded, s.Connections), line.Version)
        {
            Mirror = line.Mirror?.ToString(),
            MirrorError = line.MirrorError,
            StatusSince = line.StatusSince,
            LastReceivedAt = line.LastReceivedAt
        };
    }

    public static BoardDto ToDto(SimBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var s = board.Stats;
        return new BoardDto(
            board.Id, board.Line, board.Address, board.Name, board.ManufacturerKey, ToDto(board.Profile), board.Design, ToDto(board.Fault),
            board.AutoCreated, new BoardStatsDto(s.Frames, s.Acks, s.Naks, s.Unanswered, s.LastFrameAt),
            ToDto(board.Display, board.ManufacturerKey), board.Version)
        {
            SecondAddress = board.SecondAddress,
            Mismatch = board.Mismatch is { } m ? new ManufacturerMismatchDto(m.Evidence, m.Suggested.ToArray()) : null
        };
    }

    public static BoardProfileDto ToDto(BoardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new BoardProfileDto
        {
            Width = profile.Width,
            Rows = profile.Rows,
            RowHeight = profile.RowHeight,
            FontFile = profile.FontFile,
            FontMap = profile.FontMap,
            AlternationMs = profile.AlternationMs,
            BlinkOnMs = profile.BlinkOnMs,
            BlinkOffMs = profile.BlinkOffMs,
            ScrollSpeed = profile.ScrollSpeed,
            Monochrome = profile.Monochrome,
            SwapRedGreen = profile.SwapRedGreen,
            Header = profile.Header is { } h
                ? new BoardHeaderDto
                {
                    Height = h.Height,
                    Texts = h.Texts.Select(t => new HeaderTextDto(t.X, t.Y, t.Height, t.Text)).ToArray(),
                    Clock = h.Clock is { } c ? new HeaderClockDto(c.X, c.Y, c.Height) : null
                }
                : null,
            MovingFont = profile.MovingFont,
            AutoSize = profile.AutoSize,
            RotationMs = profile.RotationMs,
            Flaps = profile.Flaps is { } f ? ToDto(f) : null
        };
    }

    public static FlapProfileDto ToDto(FlapProfile flaps)
    {
        ArgumentNullException.ThrowIfNull(flaps);
        return new FlapProfileDto
        {
            LinesPerRecord = flaps.LinesPerRecord,
            Modules = flaps.Modules.Select(m => new FlapModuleDto(m.Line, m.Position, m.Span, m.List)).ToArray(),
            Lists = flaps.Lists,
            Colors = flaps.Colors
        };
    }

    public static BoardFaultDto ToDto(BoardFault fault)
    {
        ArgumentNullException.ThrowIfNull(fault);
        return new BoardFaultDto(fault.Kind switch
        {
            Core.Boards.FaultKind.None => Contracts.Boards.FaultKind.None,
            Core.Boards.FaultKind.NoReply => Contracts.Boards.FaultKind.NoReply,
            Core.Boards.FaultKind.Nak => Contracts.Boards.FaultKind.Nak,
            Core.Boards.FaultKind.Delay => Contracts.Boards.FaultKind.Delay,
            Core.Boards.FaultKind.Garbage => Contracts.Boards.FaultKind.Garbage,
            Core.Boards.FaultKind.Status => Contracts.Boards.FaultKind.Status,
            _ => throw new ArgumentOutOfRangeException(nameof(fault), fault.Kind, null)
        }, fault.DelayMs, fault.Status);
    }

    public static FrameDto ToDto(FrameRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new FrameDto(record.Seq, record.Time, record.Line,
            record.Direction == Core.Protocols.FrameDirection.Sent ? Contracts.Frames.FrameDirection.Sent : Contracts.Frames.FrameDirection.Received,
            record.Address, Convert.ToHexString(record.Bytes),
            record.Status switch
            {
                Core.Protocols.FrameStatus.Ok => Contracts.Frames.FrameStatus.Ok,
                Core.Protocols.FrameStatus.BadChecksum => Contracts.Frames.FrameStatus.BadChecksum,
                Core.Protocols.FrameStatus.NoBoard => Contracts.Frames.FrameStatus.NoBoard,
                Core.Protocols.FrameStatus.Fault => Contracts.Frames.FrameStatus.Fault,
                Core.Protocols.FrameStatus.Noise => Contracts.Frames.FrameStatus.Noise,
                Core.Protocols.FrameStatus.EotInData => Contracts.Frames.FrameStatus.EotInData,
                _ => throw new ArgumentOutOfRangeException(nameof(record), record.Status, null)
            },
            record.Info.Kind, record.Info.Args);
    }

    public static ReplayStatusDto ToDto(ReplayStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return new ReplayStatusDto(status.Name, status.State switch
        {
            Core.Recording.ReplayState.Running => Contracts.Recordings.ReplayState.Running,
            Core.Recording.ReplayState.Finished => Contracts.Recordings.ReplayState.Finished,
            Core.Recording.ReplayState.Stopped => Contracts.Recordings.ReplayState.Stopped,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status.State, null)
        }, status.Position, status.Duration, status.Speed, status.Loop, status.Pass);
    }

    public static BoardTemplateDto ToDto(BoardTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return new BoardTemplateDto(template.File, template.Name, ToDto(template.Profile));
    }

    public static MonitorLookDto ToDto(MonitorLook look)
    {
        ArgumentNullException.ThrowIfNull(look);
        return new MonitorLookDto(look.FontName, look.FontColor, look.HeaderColor, look.DisplayColor);
    }

    public static StationImportedDto ToDto(StationImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new StationImportedDto(result.LinesAdded, result.LinesUpdated, result.BoardsAdded, result.BoardsKept,
            result.UnsupportedLines.ToArray(), result.SkippedBoards.ToArray());
    }

    /// <summary>Nastavenie servera; <paramref name="paths" /> doplni subor databazy.</summary>
    public static SettingsDto ToDto(AppSettings settings, AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(paths);
        return new SettingsDto(settings.HttpPort, settings.AllowNetwork, settings.FontsFolder, settings.DesignsFolder, settings.ConnectHost, paths.DatabaseFile)
        {
            MonitorFolder = settings.MonitorFolder,
            RequireLogin = settings.RequireLogin,
            VirtualFolders = settings.VirtualFolders,
            InissViewFolder = settings.InissViewFolder,
            Alerts = Alerts.AlertSettingsMapper.ToDto(settings.Alerts),
            InissProgram = settings.InissProgram,
            InissRegistry = settings.InissRegistry,
            InissElevated = settings.InissElevated,
            HttpsPort = settings.HttpsPort,
            HttpsRedirect = settings.HttpsRedirect,
            HttpsCertificate = settings.HttpsCertificate,
            HttpsKey = settings.HttpsKey,
            HttpsPasswordSet = settings.HttpsPassword.Length > 0
        };
    }

    private static Contracts.Lines.LineMode ToDto(Core.Lines.LineMode mode) => mode switch
    {
        Core.Lines.LineMode.Display => Contracts.Lines.LineMode.Display,
        Core.Lines.LineMode.Monitor => Contracts.Lines.LineMode.Monitor,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    private static Contracts.Lines.LineStatus ToDto(Core.Lines.LineStatus status) => status switch
    {
        Core.Lines.LineStatus.Stopped => Contracts.Lines.LineStatus.Stopped,
        Core.Lines.LineStatus.Listening => Contracts.Lines.LineStatus.Listening,
        Core.Lines.LineStatus.Connected => Contracts.Lines.LineStatus.Connected,
        Core.Lines.LineStatus.Error => Contracts.Lines.LineStatus.Error,
        Core.Lines.LineStatus.Lost => Contracts.Lines.LineStatus.Lost,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static DisplayDto ToDto(BoardDisplay display, string manufacturer) => display switch
    {
        CharDisplay c => new DisplayDto("chars", c.Brightness, c.ClockValue, c.ClockSetAt, [], 0, null, null)
        {
            CharRows = c.Rows.Where(r => !r.Value.IsEmpty)
                .Select(r => new CharRowDto(r.Key, r.Value.Layers.Select(l => ToDto(l.Key, l.Value, manufacturer)).ToArray())).ToArray()
        },
        ElenDisplay e => new DisplayDto("elen", e.Brightness, e.ClockValue, e.ClockSetAt,
            e.Rows.OrderByDescending(r => r.Key).Select(r => new RowDto(r.Key, r.Value.Elements.Select(ToDto).ToArray())).ToArray(),
            e.MovingRow, e.MovingCodes?.Select(c => (int)c).ToArray(), e.MovingText)
        {
            BrightnessGreen = e.BrightnessGreen,
            FlashOnMs = e.FlashOn * 100,
            FlashOffMs = e.FlashOff * 100
        },
        FlapDisplay f => new DisplayDto("flaps", f.Brightness, f.ClockValue, f.ClockSetAt, [], 0, null, null)
        {
            FlapRows = f.Rows.Where(r => r.Value.Trim().Length > 0).OrderBy(r => r.Key).Select(r => new FlapRowDto(r.Key, r.Value)).ToArray(),
            RotatingUntil = f.RotatingUntil
        },
        AmplifierDisplay a => new DisplayDto("amplifier", a.Brightness, a.ClockValue, a.ClockSetAt, [], 0, null, null)
        {
            Amplifier = new AmplifierDto(a.Band, a.State, a.OnSince, a.Switches, a.LastCommand)
        },
        _ => new DisplayDto("unknown", display.Brightness, display.ClockValue, display.ClockSetAt, [], 0, null, null)
    };

    private static CharLayerDto ToDto(int layer, CharCell[] cells, string manufacturer)
    {
        var codes = cells.Select(c => c.IsSet ? (byte)c.Code : (byte)' ').ToArray();
        return new CharLayerDto(layer, cells.Select(c => (int)c.Code).ToArray(), cells.Select(c => (int)c.Font).ToArray(),
            ElektrocasCharset.Decode(codes, manufacturer));
    }

    private static ElementDto ToDto(RowElement element) => element switch
    {
        RunElement r => new ElementDto("run", ToDto(r.Run), null),
        AlternationElement a => new ElementDto("alt", null, a.Parts.Select(p => p.Select(ToDto).ToArray()).ToArray()),
        _ => throw new ArgumentOutOfRangeException(nameof(element))
    };

    private static RunDto ToDto(TextRun run) => new(run.X, run.Font, run.Codes.Select(c => (int)c).ToArray(), run.Text);
}
