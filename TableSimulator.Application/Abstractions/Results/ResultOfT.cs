namespace TableSimulator.Application.Abstractions.Results;

/// <summary>Vysledok s hodnotou pri uspechu.</summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) =>
        _value = value;

    /// <summary>Hodnota; pri chybe vynimka.</summary>
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("The value of a failure result can't be accessed.");
}
