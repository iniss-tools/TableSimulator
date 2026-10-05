namespace TableSimulator.Contracts.Auth;

/// <summary>Zalozenie prveho spravcu (POST /api/auth/setup) - len kym nie je ziaden pouzivatel a len z tohto pocitaca.</summary>
public sealed record SetupRequest(string UserName, string DisplayName, string Password);
