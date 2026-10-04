using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Api;

/// <summary>Linka pre webove rozhranie.</summary>
public sealed record LineDto(
    int Number,
    string Name,
    int TableClass,
    string Endpoint,
    LineMode Mode,
    bool AutoLearn,
    string Manufacturer,
    LineStatus Status,
    string? StatusDetail,
    string InissTablePort,
    LineStats Stats,
    long Version)
{
    /// <summary>Seriovy port zrkadla; null = bez zrkadla.</summary>
    public string? Mirror { get; init; }

    /// <summary>Chyba zrkadla; null = v poriadku.</summary>
    public string? MirrorError { get; init; }
}

/// <summary>Usek textu - bajty ako cisla (nie base64), aby ich prehliadac priamo pouzil ako indexy znakov pisma.</summary>
public sealed record RunDto(int? X, int Font, int[] Codes, string Text);

/// <summary>Prvok riadku: <c>run</c> alebo <c>alt</c> (striedanie).</summary>
public sealed record ElementDto(string Type, RunDto? Run, RunDto[][]? Parts);

/// <summary>Riadok tabule ELEN; <see cref="Row" /> cislovany odspodu.</summary>
public sealed record RowDto(int Row, ElementDto[] Elements);

/// <summary>Obsah tabule.</summary>
public sealed record DisplayDto(
    string Family,
    int Brightness,
    DateTime? Clock,
    DateTimeOffset? ClockSetAt,
    RowDto[] Rows,
    int MovingRow,
    int[]? MovingCodes,
    string? MovingText)
{
    /// <summary>Jas zelenej 0-100.</summary>
    public int BrightnessGreen { get; init; } = 100;

    /// <summary>Svietenie blikajuceho textu z prikazu 9 [ms]; null = podla profilu.</summary>
    public int? FlashOnMs { get; init; }

    /// <summary>Zhasnutie blikajuceho textu z prikazu 9 [ms]; null = podla profilu.</summary>
    public int? FlashOffMs { get; init; }
}

/// <summary>Tabula pre webove rozhranie.</summary>
public sealed record BoardDto(
    string Id,
    int Line,
    int Address,
    string Name,
    string Manufacturer,
    BoardProfile Profile,
    string? Design,
    BoardFault Fault,
    bool AutoCreated,
    BoardStats Stats,
    DisplayDto Display,
    long Version)
{
    /// <summary>Druha adresa panelu; null = len jedna.</summary>
    public int? SecondAddress { get; init; }
}

/// <summary>Zaznam komunikacie - bajty ako hex.</summary>
public sealed record FrameDto(
    long Seq,
    DateTimeOffset Time,
    int Line,
    FrameDirection Direction,
    int? Address,
    string Hex,
    FrameStatus Status,
    string Kind,
    IReadOnlyDictionary<string, string>? Args);

/// <summary>Cely stav pre prve nacitanie stranky.</summary>
public sealed record StateDto(LineDto[] Lines, BoardDto[] Boards, int[] SupportedClasses, string[] Manufacturers, SettingsDto Settings);

/// <summary>Nastavenie servera pre webove rozhranie.</summary>
public sealed record SettingsDto(int HttpPort, bool AllowNetwork, string FontsFolder, string DesignsFolder, string ConnectHost, string ConfigPath)
{
    /// <summary>Priecinok programu ELEN Monitor.</summary>
    public string MonitorFolder { get; init; } = "";
}

/// <summary>
/// Prevod stavu simulatora na DTO - vola sa pod zamkom simulatora.
/// </summary>
public static class Dto
{
    /// <summary>Vyrobcovia, ktorych tabule simulator zobrazi.</summary>
    public static string[] Manufacturers { get; } = ["ELEN", "ELENOLD", "ELEN10", "ELEN16", "ELEN16Kam", "ELEKON"];

    public static LineDto From(SimLine l, string? connectHost) => new(
        l.Number, l.Name, l.TableClass, l.Endpoint.ToString(), l.Mode, l.AutoLearn, l.Manufacturer, l.Status, l.StatusDetail,
        l.Endpoint.ToInissTablePort(l.Number, string.IsNullOrWhiteSpace(connectHost) ? null : connectHost), l.Stats, l.Version)
    {
        Mirror = l.Mirror?.ToString(),
        MirrorError = l.MirrorError
    };

    public static BoardDto From(SimBoard b) => new(
        b.Id, b.Line, b.Address, b.Name, b.ManufacturerKey, b.Profile, b.Design, b.Fault, b.AutoCreated, b.Stats, From(b.Display), b.Version)
    {
        SecondAddress = b.SecondAddress
    };

    public static FrameDto From(FrameRecord r) => new(
        r.Seq, r.Time, r.Line, r.Direction, r.Address, Convert.ToHexString(r.Bytes), r.Status, r.Info.Kind, r.Info.Args);

    private static DisplayDto From(BoardDisplay display) => display switch
    {
        ElenDisplay e => new DisplayDto("elen", e.Brightness, e.ClockValue, e.ClockSetAt,
            e.Rows.OrderByDescending(r => r.Key).Select(r => new RowDto(r.Key, r.Value.Elements.Select(From).ToArray())).ToArray(),
            e.MovingRow, e.MovingCodes?.Select(c => (int)c).ToArray(), e.MovingText)
        {
            BrightnessGreen = e.BrightnessGreen,
            FlashOnMs = e.FlashOn * 100,
            FlashOffMs = e.FlashOff * 100
        },
        _ => new DisplayDto("unknown", display.Brightness, display.ClockValue, display.ClockSetAt, [], 0, null, null)
    };

    private static ElementDto From(RowElement element) => element switch
    {
        RunElement r => new ElementDto("run", From(r.Run), null),
        AlternationElement a => new ElementDto("alt", null, a.Parts.Select(p => p.Select(From).ToArray()).ToArray()),
        _ => throw new ArgumentOutOfRangeException(nameof(element))
    };

    private static RunDto From(TextRun run) => new(run.X, run.Font, run.Codes.Select(c => (int)c).ToArray(), run.Text);
}
