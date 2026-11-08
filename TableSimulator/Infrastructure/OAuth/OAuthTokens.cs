namespace TableSimulator.Infrastructure.OAuth;

/// <summary>Tokeny vydane klientovi (odpoved /oauth/token).</summary>
public sealed record OAuthTokens(string AccessToken, string RefreshToken, TimeSpan ExpiresIn);
