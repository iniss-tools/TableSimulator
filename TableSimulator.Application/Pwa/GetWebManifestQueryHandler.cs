using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Pwa;

namespace TableSimulator.Application.Pwa;

/// <summary>
/// Manifest kiosku: aplikacia sa po spusteni otvori na stranke kiosku (<c>/?view=…</c> - web ju prevedie na adresu
/// za #, ktoru niektore prehliadace v start_url zahadzuju) a s klucom zariadenia, na celu obrazovku.
/// </summary>
public sealed class GetWebManifestQueryHandler : IQueryHandler<GetWebManifestQuery, WebManifestDto>
{
    /// <summary>Ikony v wwwroot (PNG pre Android a iOS, SVG pre ostatne).</summary>
    private static readonly WebManifestIconDto[] Icons =
    [
        new("/icon-192.png", "192x192", "image/png", "any"),
        new("/icon-512.png", "512x512", "image/png", "any"),
        new("/icon-maskable-512.png", "512x512", "image/png", "maskable"),
        new("/favicon.svg", "any", "image/svg+xml", "any")
    ];

    public Task<Result<WebManifestDto>> Handle(GetWebManifestQuery query, CancellationToken cancellationToken)
    {
        var id = "/?view=" + Uri.EscapeDataString(query.View);
        var start = string.IsNullOrEmpty(query.Key) ? id : id + "&key=" + query.Key;
        var name = string.IsNullOrWhiteSpace(query.Name) ? "TableSimulator" : query.Name.Trim();
        return Task.FromResult(Result.Success(new WebManifestDto
        {
            Id = id, Name = name, ShortName = name.Length <= 12 ? name : name[..12].TrimEnd(), StartUrl = start, Icons = Icons
        }));
    }
}
