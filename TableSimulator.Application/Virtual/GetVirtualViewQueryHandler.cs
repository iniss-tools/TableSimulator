using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Application.Virtual;

public sealed class GetVirtualViewQueryHandler(IVirtualBoardRepository boards, XmlTableFolders folders, TimeProvider time)
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
        return Result.Success(new VirtualViewDto(DtoMapper.ToDto(board), source is null ? null : DtoMapper.ToDto(source, time.GetLocalNow().DateTime), rows));
    }
}
