using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

/// <summary>Co tabule ukazuju ako text, zoradene podla linky a adresy; linka null = vsetky.</summary>
public sealed record GetBoardTextsQuery(int? Line) : IQuery<IReadOnlyList<BoardTextDto>>;

public sealed class GetBoardTextsQueryValidator : AbstractValidator<GetBoardTextsQuery>
{
    public GetBoardTextsQueryValidator() => RuleFor(q => q.Line).InclusiveBetween(1, 100).When(q => q.Line is not null).WithErrorCode(BoardErrors.Line);
}

public sealed class GetBoardTextsQueryHandler(Simulator simulator) : IQueryHandler<GetBoardTextsQuery, IReadOnlyList<BoardTextDto>>
{
    public Task<Result<IReadOnlyList<BoardTextDto>>> Handle(GetBoardTextsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<BoardTextDto> texts = simulator.Read(s => s.Boards.Where(b => query.Line is null || b.Line == query.Line).Select(DtoMapper.ToTextDto).ToList());
        return Task.FromResult(Result.Success(texts));
    }
}
