using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Application.Mapping;

/// <summary>Virtualne tabule a XML tabul INISSu.</summary>
public static partial class DtoMapper
{
    public static VirtualBoardDto ToDto(VirtualBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return new VirtualBoardDto(board.Id, board.Name, board.Description, board.Source, board.RowCount, board.RowStart, board.Separator,
            board.Template, board.Parameter, board.Layout);
    }

    /// <summary>Subor XML tabule; <paramref name="now" /> urci, ci uz jeho obsah neplati.</summary>
    public static VirtualSourceDto ToDto(XmlTableSource source, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(source);
        var table = source.Table;
        return new VirtualSourceDto(source.Name, source.Path, source.Modified, table?.Station, table?.Key, table?.Written, table?.ValidTo,
            table?.Rows.Count ?? 0, source.IsStale(now), source.Error);
    }

    public static VirtualRowDto ToDto(XmlTableRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new VirtualRowDto(row.Cells.Select(c => new VirtualCellDto(c.Name, c.Text)).ToArray());
    }
}
