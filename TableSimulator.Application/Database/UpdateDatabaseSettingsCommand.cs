using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Application.Database;

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
