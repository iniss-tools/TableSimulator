namespace TableSimulator.Contracts.Signage;

/// <summary>Nahraty subor spravy - obrazok alebo video (POST /api/signage/media).</summary>
/// <param name="Id">identifikator (<c>GET /api/signage/media/{id}</c>)</param>
/// <param name="Name">povodne meno suboru</param>
/// <param name="ContentType">typ obsahu</param>
/// <param name="Size">velkost v bajtoch</param>
public sealed record SignageMediaDto(Guid Id, string Name, string ContentType, long Size);
