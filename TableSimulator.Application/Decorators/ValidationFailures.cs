using FluentValidation;
using FluentValidation.Results;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Decorators;

/// <summary>Spustenie validatorov a prevod ich chyb na <see cref="ValidationError" />.</summary>
internal static class ValidationFailures
{
    public static async Task<ValidationFailure[]> ValidateAsync<T>(T request, IEnumerable<IValidator<T>> validators, CancellationToken cancellationToken)
    {
        var list = validators.ToList();
        if (list.Count == 0)
            return [];

        var context = new ValidationContext<T>(request);
        var results = await Task.WhenAll(list.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
        return results.Where(r => !r.IsValid).SelectMany(r => r.Errors).ToArray();
    }

    public static ValidationError ToError(ValidationFailure[] failures) =>
        new(failures.Select(f => Error.Problem(f.ErrorCode, f.ErrorMessage)).ToArray());
}
