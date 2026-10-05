using Microsoft.Extensions.Logging;
using TableSimulator.Application.Auth;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Zaznamy prihlasovania.</summary>
internal static partial class AuthLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Prihlasovanie: {Mode}")]
    public static partial void Mode(ILogger logger, string mode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Prihlaseny {UserName} ({Role})")]
    public static partial void SignedIn(ILogger logger, string userName, string role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Neuspesne prihlasenie {UserName}: {Outcome}")]
    public static partial void SignInFailed(ILogger logger, string userName, SignInOutcome outcome);

    [LoggerMessage(Level = LogLevel.Information, Message = "Odhlaseny {UserName}")]
    public static partial void SignedOut(ILogger logger, string? userName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Neplatny pristupovy kluc {Prefix}…")]
    public static partial void KeyRejected(ILogger logger, string prefix);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Zalozeny prvy spravca {UserName}")]
    public static partial void AdminCreated(ILogger logger, string userName);
}
