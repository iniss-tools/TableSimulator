using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Station;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Application.Mapping;

/// <summary>Definicie virtualnych tabul z poziadaviek a z INISSView.</summary>
public static partial class RequestMapper
{
    /// <summary>Virtualna tabula s cislom z adresy poziadavky.</summary>
    public static VirtualBoard ToVirtualBoard(int id, SetVirtualBoardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new VirtualBoard(id, request.Name.Trim(), (request.Description ?? "").Trim(), request.Source.Trim(), request.RowCount,
            request.RowStart, request.Separator ?? "", (request.Template ?? "").Trim(), (request.Parameter ?? "").Trim(), (request.Layout ?? "").Trim());
    }

    /// <summary>
    /// Virtualna tabula z definicie INISSView. Popis, ktory sa zhoduje s nazvom, sa vynecha; texty a pocty sa zmestia
    /// do medzi rovnakych ako pri zadani v simulatore. Rozlozenie sa odhadne podla mena sablony XSL.
    /// </summary>
    public static VirtualBoard ToVirtualBoard(InissViewTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var name = Limit(table.Name.Length > 0 ? table.Name : table.Xml, 100);
        return new VirtualBoard(table.Number, name, table.Title == table.Name ? "" : Limit(table.Title, 200), Limit(table.Xml, 100),
            table.RowCount is > VirtualErrors.MaxRows ? null : table.RowCount, Math.Clamp(table.RowStart, 1, VirtualErrors.MaxRows),
            Limit(table.Separator, 20), Limit(table.Template, 100), Limit(table.Parameter, 200), VirtualLayouts.Guess(table.Template));
    }

    /// <summary>Virtualna tabula pre tabulu INISSu s exportom do XML (z importu stanice) - vsetky zaznamy.</summary>
    public static VirtualBoard ToVirtualBoard(int id, StationXmlTableDto table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var source = table.Source.Trim();
        var name = string.IsNullOrWhiteSpace(table.Name) ? source : table.Name.Trim();
        return new VirtualBoard(id, Limit(name, 100), "", source, null, 1, "", "", "", VirtualLayouts.Guess(name));
    }

    private static string Limit(string text, int length) => text.Length <= length ? text : text[..length];
}
