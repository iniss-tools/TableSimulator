namespace TableSimulator.Application.OAuth;

/// <summary>Ziadost klienta o povolenie cakajuca na rozhodnutie pouzivatela na stranke povolenia (<c>#/authorize/{Id}</c>).</summary>
/// <param name="Id">nahodny identifikator v adrese stranky</param>
/// <param name="Client">klient</param>
/// <param name="RedirectUri">adresa navratu z ziadosti (jedna z registrovanych)</param>
/// <param name="CodeChallenge">vyzva PKCE S256</param>
/// <param name="State">hodnota klienta, ktoru dostane spat</param>
/// <param name="Issuer">adresa servera - ide spat ako <c>iss</c> (RFC 9207)</param>
/// <param name="ExpiresAt">koniec platnosti (UTC)</param>
public sealed record OAuthRequest(string Id, OAuthClient Client, string RedirectUri, string CodeChallenge, string? State, string Issuer, DateTime ExpiresAt);
