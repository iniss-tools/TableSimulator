using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Decorators;

/// <summary>Overi dotaz validatormi FluentValidation; pri chybe handler nespusti.</summary>
public sealed class ValidationQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> innerHandler,
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var failures = await ValidationFailures.ValidateAsync(query, validators, cancellationToken).ConfigureAwait(false);
        return failures.Length == 0
            ? await innerHandler.Handle(query, cancellationToken).ConfigureAwait(false)
            : Result.Failure<TResponse>(ValidationFailures.ToError(failures));
    }
}
