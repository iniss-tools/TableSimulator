namespace TableSimulator.Contracts.Users;

/// <summary>Nove heslo pouzivatela od spravcu (POST /api/users/{meno}/password).</summary>
public sealed record SetPasswordRequest(string Password);
