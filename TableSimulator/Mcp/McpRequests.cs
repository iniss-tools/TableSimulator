using System.Globalization;
using ModelContextProtocol;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Lines;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;
using FaultKind = TableSimulator.Contracts.Boards.FaultKind;

namespace TableSimulator.Mcp;

/// <summary>
/// Poziadavky zmien z parametrov nastroja: pri existujucej linke alebo tabuli sa zmeni len to, co model poslal (null = ponechat),
/// nova dostane predvolene hodnoty ako vo webovom rozhrani.
/// </summary>
public static class McpRequests
{
    public static SetLineRequest Line(LineDto? current, int number, int? tableClass, string? endpoint, string? name, LineMode? mode, bool? autoLearn,
        string? manufacturer)
    {
        if (current is null && (tableClass is null || string.IsNullOrWhiteSpace(endpoint)))
            throw new McpException(string.Create(CultureInfo.InvariantCulture, $"line.new: Line {number} does not exist; a new line needs tableClass and endpoint."));
        var tableClassValue = tableClass ?? current!.TableClass;
        return new SetLineRequest
        {
            Number = number,
            TableClass = tableClassValue,
            Endpoint = endpoint ?? current!.Endpoint,
            Name = name ?? current?.Name ?? "",
            Mode = mode ?? current?.Mode ?? LineMode.Display,
            AutoLearn = autoLearn ?? current?.AutoLearn ?? true,
            // nova linka: prvy vyrobca triedy (ako ponuka webu)
            Manufacturer = manufacturer ?? current?.Manufacturer ?? (Simulator.ManufacturersFor(tableClassValue) is [var first, ..] ? first : ""),
            Mirror = current?.Mirror
        };
    }

    /// <summary>Tabula; <paramref name="secondAddress" /> 0 = bez druhej adresy, null = ponechat.</summary>
    public static SetBoardRequest Board(BoardDto? current, int line, int address, string? manufacturer, string? name, int? width, int? rows, int? rowHeight,
        int? secondAddress)
    {
        if (current is null && string.IsNullOrWhiteSpace(manufacturer))
            throw new McpException(string.Create(CultureInfo.InvariantCulture, $"board.new: Board {line}-{address} does not exist; a new board needs a manufacturer."));
        var manufacturerValue = manufacturer ?? current!.Manufacturer;
        var profile = current?.Profile ?? DtoMapper.ToDto(BoardProfile.DefaultFor(manufacturerValue));
        return new SetBoardRequest
        {
            Line = line,
            Address = address,
            Manufacturer = manufacturerValue,
            Name = name ?? current?.Name ?? "",
            Profile = profile with { Width = width ?? profile.Width, Rows = rows ?? profile.Rows, RowHeight = rowHeight ?? profile.RowHeight },
            Design = current?.Design,
            Fault = current?.Fault ?? new BoardFaultDto(FaultKind.None),
            SecondAddress = secondAddress is 0 ? null : secondAddress ?? current?.SecondAddress
        };
    }
}
