using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Settings;

namespace TableSimulator.Mcp;

/// <summary>
/// Zdroj MCP Apps so zivou tabulou - jeden subor HTML zo zostavy webu (<c>web/vite.mcp.config.ts</c> → <c>wwwroot/mcp-app/board.html</c>),
/// ktory klient ukaze v izolovanom ramci; udaje si berie z nastroja board_view.
/// </summary>
[McpServerResourceType]
public static class BoardApp
{
    public const string Uri = "ui://tablesimulator/board.html";

    /// <summary>Zostava bez webu (testy so SkipWebBuild) - ramec povie, co chyba.</summary>
    private const string Missing =
        "<!doctype html><html><body style=\"font-family:sans-serif\">The board view is not built - build the web (bun run build).</body></html>";

    [McpServerResource(UriTemplate = Uri, Name = "board-view", Title = "Board view", MimeType = McpApps.HtmlMimeType)]
    [Description("Live view of a board for show_board in clients with MCP Apps.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static async Task<string> Html(AppPaths paths, CancellationToken cancellationToken)
    {
        var file = Path.Combine(paths.WebRoot, "mcp-app", "board.html");
        return File.Exists(file) ? await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false) : Missing;
    }
}
