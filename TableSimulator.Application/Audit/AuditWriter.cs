using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Audit;

/// <summary>
/// Zapise vybaveny prikaz do auditu - kto, kedy, odkial, co, cim sa to tykalo a ci sa to podarilo. Chyba zapisu
/// prikaz nepokazi (len sa zaznamena).
/// </summary>
public sealed partial class AuditWriter(IAuditLog log, ICurrentUser user, TimeProvider time, ILogger<AuditWriter> logger)
{
    private const string Suffix = "Command";

    /// <summary>Meno akcie z typu prikazu (<c>SetLineCommand</c> = <c>SetLine</c>).</summary>
    public static string ActionOf(Type command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command.Name.EndsWith(Suffix, StringComparison.Ordinal) ? command.Name[..^Suffix.Length] : command.Name;
    }

    public async Task WriteAsync<TCommand>(TCommand command, Result result)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(result);
        var entry = new AuditEntry(0, time.GetUtcNow().UtcDateTime, user.UserName ?? "-", ActionOf(typeof(TCommand)),
            (command as IAuditable)?.AuditTarget ?? "", result.IsSuccess, result.IsSuccess ? null : result.Error.Code, AuditDetails.Of(command),
            user.Address);
        try
        {
            // zaznam patri aj prikazu, ktory klient medzicasom zrusil
            await log.WriteAsync(entry, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            WriteFailed(logger, e, entry.Action);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Zaznam auditu {Action} sa nepodarilo zapisat")]
    private static partial void WriteFailed(ILogger logger, Exception exception, string action);
}
