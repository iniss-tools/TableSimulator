namespace TableSimulator.Contracts.Users;

/// <summary>Zmena pouzivatela (PUT /api/users/{meno}).</summary>
public sealed record UpdateUserRequest(string DisplayName, string Role, bool Disabled);
