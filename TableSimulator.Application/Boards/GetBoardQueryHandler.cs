using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

public sealed class GetBoardQueryHandler(Simulator simulator) : IQueryHandler<GetBoardQuery, BoardDto>
{
    public Task<Result<BoardDto>> Handle(GetBoardQuery query, CancellationToken cancellationToken)
    {
        var board = simulator.Read(s => s.FindBoard(query.Id) is { } b ? DtoMapper.ToDto(b) : null);
        return Task.FromResult(board is null ? Result.Failure<BoardDto>(BoardErrors.NotFound(query.Id)) : Result.Success(board));
    }
}
