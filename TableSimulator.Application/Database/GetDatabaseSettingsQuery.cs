using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Database;

namespace TableSimulator.Application.Database;

/// <summary>Pouzivana a vybrana databaza.</summary>
public sealed record GetDatabaseSettingsQuery : IQuery<DatabaseSettingsDto>;

public sealed class GetDatabaseSettingsQueryHandler(IDatabaseSettings database) : IQueryHandler<GetDatabaseSettingsQuery, DatabaseSettingsDto>
{
    public Task<Result<DatabaseSettingsDto>> Handle(GetDatabaseSettingsQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(database.Get()));
}
