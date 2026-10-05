using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Mapping;

/// <summary>Prevod poziadaviek API na nastavenie simulatora.</summary>
public static class RequestMapper
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
            MovingFont = profile.MovingFont,
            AutoSize = profile.AutoSize
        };
    }

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
            Lines = request.Lines.Select(l => new StationLine(l.Number, l.TableClass, l.Port) { Name = l.Name }).ToList(),
            Boards = request.Boards.Select(b => new StationBoard(b.Line, b.Address, b.Manufacturer) { Name = b.Name }).ToList()
        };
    }
}
