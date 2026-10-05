using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Frames;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Frames;

public sealed class GetLogQueryHandler(Simulator simulator) : IQueryHandler<GetLogQuery, FrameDto[]>
{
    public Task<Result<FrameDto[]>> Handle(GetLogQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(simulator.LogSince(query.After).Select(DtoMapper.ToDto).ToArray()));
}
