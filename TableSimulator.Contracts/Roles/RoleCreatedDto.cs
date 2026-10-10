namespace TableSimulator.Contracts.Roles;

/// <summary>Zalozena rola.</summary>
/// <param name="Name">kluc novej roly (z nazvu bez diakritiky)</param>
public sealed record RoleCreatedDto(string Name);
