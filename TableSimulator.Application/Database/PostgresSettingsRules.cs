using System.Data.Common;
using FluentValidation;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Application.Database;

/// <summary>Pripojenie k PostgreSQL: server, port, databaza a dalsie parametre bez hesla.</summary>
public sealed class PostgresSettingsRules : AbstractValidator<PostgresSettingsRequest>
{
    // tieto parametre maju vlastne polia; heslo v parametroch by ostalo v subore citatelne
    private static readonly string[] OwnFields = ["host", "server", "port", "database", "db", "username", "user id", "userid", "user", "password", "pwd"];

    public PostgresSettingsRules()
    {
        RuleFor(r => r.Host).Must(h => !string.IsNullOrWhiteSpace(h) && !h.Trim().Any(char.IsWhiteSpace)).WithErrorCode(DatabaseErrors.Host)
            .WithMessage("Enter the PostgreSQL server (without spaces).");
        RuleFor(r => r.Port).InclusiveBetween(1, 65535).WithErrorCode(DatabaseErrors.Port);
        RuleFor(r => r.Database).Must(d => !string.IsNullOrWhiteSpace(d)).WithErrorCode(DatabaseErrors.Name).WithMessage("Enter the database name.");
        RuleFor(r => r.Options).Must(ValidOptions).WithErrorCode(DatabaseErrors.Options)
            .WithMessage("Additional parameters must be 'name=value' pairs separated by semicolons, without server, user or password.");
    }

    private static bool ValidOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
            return true;
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = options };
            return !builder.Keys.Cast<string>().Any(k => OwnFields.Contains(k.Trim().ToLowerInvariant()));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
