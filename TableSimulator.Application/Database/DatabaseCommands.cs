using System.Data.Common;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Application.Database;

/// <summary>
/// Vyber databazy simulatora (implementuje server - subor <c>database.json</c> mimo databazy): SQLite alebo PostgreSQL.
/// Zmena sa prejavi po restarte servera; udaje sa medzi databazami prenasaju zalohou a obnovou.
/// </summary>
public interface IDatabaseSettings
{
    /// <summary>Pouzivana a vybrana databaza.</summary>
    DatabaseSettingsDto Get();

    /// <summary>Zapise vyber databazy (heslo null = nechat ulozene).</summary>
    void Save(UpdateDatabaseRequest request);

    /// <summary>Skusi pripojenie k PostgreSQL (heslo null = ulozene).</summary>
    Task<DatabaseTestResultDto> TestAsync(PostgresSettingsRequest request, CancellationToken cancellationToken);
}

/// <summary>Kody chyb vyberu databazy.</summary>
public static class DatabaseErrors
{
    public const string Host = "database.host";
    public const string Port = "database.port";
    public const string Name = "database.name";
    public const string Options = "database.options";
}

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

/// <summary>Pouzivana a vybrana databaza.</summary>
public sealed record GetDatabaseSettingsQuery : IQuery<DatabaseSettingsDto>;

public sealed class GetDatabaseSettingsQueryHandler(IDatabaseSettings database) : IQueryHandler<GetDatabaseSettingsQuery, DatabaseSettingsDto>
{
    public Task<Result<DatabaseSettingsDto>> Handle(GetDatabaseSettingsQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(database.Get()));
}

/// <summary>Zmeni vyber databazy; prejavi sa po restarte servera.</summary>
public sealed record UpdateDatabaseSettingsCommand(UpdateDatabaseRequest Request) : ICommand<DatabaseSettingsDto>, IAuditable
{
    string IAuditable.AuditTarget => "database";
}

public sealed class UpdateDatabaseSettingsCommandValidator : AbstractValidator<UpdateDatabaseSettingsCommand>
{
    public UpdateDatabaseSettingsCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(DatabaseErrors.Host);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Provider).IsInEnum().WithErrorCode(DatabaseErrors.Host);
            RuleFor(c => c.Request.Postgres).NotNull().WithErrorCode(DatabaseErrors.Host);
            // pri SQLite sa pripojenie k PostgreSQL len odlozi - kontroluje sa az pri jeho vybere
            RuleFor(c => c.Request.Postgres).SetValidator(new PostgresSettingsRules())
                .When(c => c.Request.Provider == DatabaseKind.Postgres && c.Request.Postgres is not null);
        });
    }
}

public sealed class UpdateDatabaseSettingsCommandHandler(IDatabaseSettings database) : ICommandHandler<UpdateDatabaseSettingsCommand, DatabaseSettingsDto>
{
    public Task<Result<DatabaseSettingsDto>> Handle(UpdateDatabaseSettingsCommand command, CancellationToken cancellationToken)
    {
        database.Save(command.Request);
        return Task.FromResult(Result.Success(database.Get()));
    }
}

/// <summary>Skusi pripojenie k PostgreSQL podla formulara (heslo null = ulozene).</summary>
public sealed record TestDatabaseCommand(PostgresSettingsRequest Request) : ICommand<DatabaseTestResultDto>, IAuditable
{
    string IAuditable.AuditTarget => "database";
}

public sealed class TestDatabaseCommandValidator : AbstractValidator<TestDatabaseCommand>
{
    public TestDatabaseCommandValidator() => RuleFor(c => c.Request).NotNull().WithErrorCode(DatabaseErrors.Host).SetValidator(new PostgresSettingsRules());
}

public sealed class TestDatabaseCommandHandler(IDatabaseSettings database) : ICommandHandler<TestDatabaseCommand, DatabaseTestResultDto>
{
    public async Task<Result<DatabaseTestResultDto>> Handle(TestDatabaseCommand command, CancellationToken cancellationToken) =>
        Result.Success(await database.TestAsync(command.Request, cancellationToken).ConfigureAwait(false));
}
