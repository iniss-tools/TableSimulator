using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Application.Users;

/// <summary>Vsetci pouzivatelia podla mena.</summary>
public sealed record GetUsersQuery : IQuery<IReadOnlyList<UserDto>>;
