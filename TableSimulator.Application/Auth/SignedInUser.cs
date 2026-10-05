namespace TableSimulator.Application.Auth;

/// <summary>Pouzivatel po prihlaseni.</summary>
public sealed record SignedInUser(string UserName, string DisplayName, string Role);
