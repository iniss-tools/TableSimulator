using Microsoft.Extensions.Logging;

namespace TableSimulator.Infrastructure.OAuth;

/// <summary>Zaznamy servera OAuth pre MCP.</summary>
internal static partial class OAuthLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "OAuth: zaregistrovany klient {Client} (navrat {RedirectUris})")]
    public static partial void Registered(ILogger logger, string client, IEnumerable<string> redirectUris);

    [LoggerMessage(Level = LogLevel.Information, Message = "OAuth: {UserName} povolil pristup klientovi {Client} s rolou {Role}")]
    public static partial void Approved(ILogger logger, string client, string userName, string role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OAuth: uz vymeneny obnovovaci token klienta {Client} ({UserName}) - povolenie zrusene")]
    public static partial void RefreshReused(ILogger logger, string client, string userName);

    [LoggerMessage(Level = LogLevel.Information, Message = "OAuth: pouzivatel {UserName} uz nemoze dat pristup klientovi {Client} - povolenie zrusene")]
    public static partial void UserNotAllowed(ILogger logger, string client, string userName);
}
