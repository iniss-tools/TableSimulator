using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Decorators;

/// <summary>Overi prikaz bez vysledku validatormi FluentValidation; pri chybe handler nespusti.</summary>
public sealed class ValidationCommandHandler<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var failures = await ValidationFailures.ValidateAsync(command, validators, cancellationToken).ConfigureAwait(false);
        return failures.Length == 0
            ? await innerHandler.Handle(command, cancellationToken).ConfigureAwait(false)
            : Result.Failure(ValidationFailures.ToError(failures));
    }
}

/// <summary>Overi prikaz s vysledkom validatormi FluentValidation; pri chybe handler nespusti.</summary>
public sealed class ValidationCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var failures = await ValidationFailures.ValidateAsync(command, validators, cancellationToken).ConfigureAwait(false);
        return failures.Length == 0
            ? await innerHandler.Handle(command, cancellationToken).ConfigureAwait(false)
            : Result.Failure<TResponse>(ValidationFailures.ToError(failures));
    }
}
