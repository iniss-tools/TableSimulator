namespace TableSimulator.Contracts.Users;

/// <summary>Zmena vlastneho hesla (POST /api/auth/password).</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
