using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Application.Virtual;

public sealed class GetVirtualViewQueryHandler(
    IVirtualBoardRepository boards,
    IVirtualLayoutRepository layouts,
    XmlTableFolders folders,
    InissViewSite inissView,
    TimeProvider time)
    : IQueryHandler<GetVirtualViewQuery, VirtualViewDto>
{
    public async Task<Result<VirtualViewDto>> Handle(GetVirtualViewQuery query, CancellationToken cancellationToken)
    {
        if (await boards.FindAsync(query.Id, cancellationToken).ConfigureAwait(false) is not { } board)
            return Result.Failure<VirtualViewDto>(VirtualErrors.NotFound(query.Id));

        var source = folders.Find(board.Source);
        var rows = source?.Table is { } table
            ? XmlTableFilter.Apply(table.Rows, board.RowCount, board.RowStart, board.Separator).Select(DtoMapper.ToDto).ToArray()
            : [];
        var layout = await LayoutOf(board, cancellationToken).ConfigureAwait(false);
        return Result.Success(new VirtualViewDto(DtoMapper.ToDto(board), source is null ? null : DtoMapper.ToDto(source, time.GetLocalNow().DateTime), rows,
            layout)
        {
            // sablona INISSView ma prednost pred rozlozenim, ked je priecinok INISSView nastaveny
            Page = board.Template.Length > 0 && inissView.Enabled ? inissView.Render(board, source) : null
        });
    }

    /// <summary>Rozlozenie tabule; ak neexistuje (alebo ziadne nema), predvolene.</summary>
    private async Task<VirtualLayoutDto> LayoutOf(VirtualBoard board, CancellationToken cancellationToken)
    {
        if (board.Layout.Length == 0)
            return VirtualLayouts.Default;
        if (VirtualLayouts.FindBuiltIn(board.Layout) is { } builtIn)
            return builtIn;
        return await layouts.FindAsync(board.Layout, cancellationToken).ConfigureAwait(false) is { } record
            ? VirtualLayouts.Deserialize(record)
            : VirtualLayouts.Default;
    }
}
