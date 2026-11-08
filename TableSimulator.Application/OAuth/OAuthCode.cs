namespace TableSimulator.Application.OAuth;

/// <summary>Jednorazovy kod po povoleni - klient ho vymeni za tokeny.</summary>
public sealed record OAuthCode(string ClientId, string RedirectUri, string CodeChallenge, string UserName, string Role, DateTime ExpiresAt);
