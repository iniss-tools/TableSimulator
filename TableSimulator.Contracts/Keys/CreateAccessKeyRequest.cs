namespace TableSimulator.Contracts.Keys;

/// <summary>Novy pristupovy kluc (POST /api/keys); kluc zariadenia ma vzdy rolu pozorovatel.</summary>
public sealed record CreateAccessKeyRequest(string Name, AccessKeyKind Kind, string Role);
