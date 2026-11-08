using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Application.Database;

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
