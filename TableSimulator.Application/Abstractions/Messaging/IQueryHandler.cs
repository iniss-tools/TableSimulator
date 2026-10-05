using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Abstractions.Messaging;

/// <summary>Vybavi dotaz.</summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
