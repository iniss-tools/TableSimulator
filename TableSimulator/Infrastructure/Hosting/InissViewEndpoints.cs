using System.Globalization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using TableSimulator.Application.Virtual;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Subory stranok virtualnych tabul zo sablon INISSView pod <c>/inissview/</c> (stranka ma <c>base</c>
/// <see cref="InissViewSite.BasePath" />): CSS, obrazky a <c>JScript.js</c> z priecinka INISSView a nahrada
/// <c>XMLStatus.aspx</c>. Bez prihlasenia - su to len subory vzhladu (bez nastavenia a programu); udaje tabule
/// posiela API v stranke.
/// </summary>
public static class InissViewEndpoints
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public static IEndpointConventionBuilder MapInissViewFiles(this IEndpointRouteBuilder app) =>
        app.MapGet("/inissview/{**path}", (string? path, InissViewSite site, TimeProvider time, HttpContext context) =>
            {
                // stranka je v koreni aplikacie (app/), obrazky sablon su o uroven vyssie (../img)
                var relative = path ?? "";
                if (relative.StartsWith("app/", StringComparison.OrdinalIgnoreCase))
                    relative = relative[4..];

                if (string.Equals(Path.GetFileName(relative), "XMLStatus.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    // skript stranky sa kazdych par sekund pyta, ci sa XML zmenilo; obsah sa nemeni - stranku obnovuje
                    // simulator sam - a cas servera zosynchronizuje hodiny stranky
                    context.Response.Headers.CacheControl = "no-store";
                    return Results.Text(string.Create(CultureInfo.InvariantCulture, $"<!--ACTTIME{time.GetLocalNow():yyyyMMddHHmmss}--><!--TableSimulator-->"),
                        "text/html");
                }

                if (site.Asset(relative) is not { } file)
                    return Results.NotFound();
                return Results.File(file, ContentTypes.TryGetContentType(file, out var type) ? type : "application/octet-stream");
            })
            .AllowAnonymous()
            .ExcludeFromDescription();
}
