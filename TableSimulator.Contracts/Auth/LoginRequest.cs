namespace TableSimulator.Contracts.Auth;

/// <summary>Prihlasenie (POST /api/auth/login).</summary>
/// <param name="Remember">zostat prihlaseny aj po zatvoreni prehliadaca</param>
public sealed record LoginRequest(string UserName, string Password, bool Remember);
