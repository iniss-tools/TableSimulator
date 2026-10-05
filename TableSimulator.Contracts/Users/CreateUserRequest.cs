namespace TableSimulator.Contracts.Users;

/// <summary>Novy pouzivatel (POST /api/users).</summary>
public sealed record CreateUserRequest(string UserName, string DisplayName, string Role, string Password);
