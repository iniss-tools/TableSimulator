namespace TableSimulator.Application.OAuth;

/// <summary>
/// Povolenie pouzivatela pre klienta MCP: klient pristupuje s rolou <paramref name="Role" /> (najviac rola pouzivatela).
/// Platny je vzdy prave jeden pristupovy a jeden obnovovaci token; ulozene su len ich hashe.
/// </summary>
/// <param name="Id">identifikator</param>
/// <param name="ClientId">klient</param>
/// <param name="ClientName">nazov klienta pri povoleni</param>
/// <param name="UserName">pouzivatel, ktory pristup povolil</param>
/// <param name="Role">rola klienta</param>
/// <param name="CreatedAt">povolenie (UTC)</param>
/// <param name="LastUsedAt">posledne pouzitie (UTC)</param>
/// <param name="AccessHash">hash pristupoveho tokenu</param>
/// <param name="AccessExpiresAt">koniec platnosti pristupoveho tokenu (UTC)</param>
/// <param name="RefreshHash">hash obnovovacieho tokenu</param>
/// <param name="RefreshExpiresAt">koniec platnosti obnovovacieho tokenu (UTC)</param>
/// <param name="PreviousRefreshHash">hash naposledy vymeneneho obnovovacieho tokenu - jeho pouzitie znamena unik</param>
public sealed record OAuthGrant(
    Guid Id,
    string ClientId,
    string ClientName,
    string UserName,
    string Role,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    string AccessHash,
    DateTime AccessExpiresAt,
    string RefreshHash,
    DateTime RefreshExpiresAt,
    string? PreviousRefreshHash);
