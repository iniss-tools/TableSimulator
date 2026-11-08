using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Infrastructure.Https;

/// <summary>Vlastny certifikat sa neda pouzit.</summary>
public sealed class CertificateLoadException(Error error) : Exception(error?.Description)
{
    /// <summary>Chyba s kodom pre obsluhu.</summary>
    public Error Error { get; } = error ?? throw new ArgumentNullException(nameof(error));
}
