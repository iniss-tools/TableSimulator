using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Settings;

namespace TableSimulator.Mcp;

/// <summary>
/// Zdroje MCP Apps so zivymi tabulami - jeden subor HTML zo zostavy webu (<c>web/vite.mcp.config.ts</c> → <c>wwwroot/mcp-app/app.html</c>),
/// ktory klient ukaze v izolovanom ramci. Kazdy zdroj ho vyda so znackou <c>data-app</c>, ktora aplikacia sa ma spustit;
/// udaje si aplikacia berie z nastrojov board_view a virtual_view.
/// </summary>
[McpServerResourceType]
public static class AppPages
{
    public const string BoardUri = "ui://tablesimulator/board.html";
    public const string VirtualUri = "ui://tablesimulator/virtual.html";

    /// <summary>Zostava bez webu (testy so SkipWebBuild) - ramec povie, co chyba.</summary>
    private const string Missing =
        "<!doctype html><html><body style=\"font-family:sans-serif\">The board view is not built - build the web (bun run build).</body></html>";

    private const string Root = "<div id=\"root\"></div>";

    [McpServerResource(UriTemplate = BoardUri, Name = "board-view", Title = "Board view", MimeType = McpApps.HtmlMimeType)]
    [Description("Live view of a board for show_board in clients with MCP Apps.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static Task<string> Board(AppPaths paths, CancellationToken cancellationToken) => Html(paths, "board", cancellationToken);

    [McpServerResource(UriTemplate = VirtualUri, Name = "virtual-board-view", Title = "Virtual board view", MimeType = McpApps.HtmlMimeType)]
    [Description("Live view of a virtual board for show_virtual_board in clients with MCP Apps.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static Task<string> Virtual(AppPaths paths, CancellationToken cancellationToken) => Html(paths, "virtual", cancellationToken);

    private static async Task<string> Html(AppPaths paths, string app, CancellationToken cancellationToken)
    {
        var file = Path.Combine(paths.WebRoot, "mcp-app", "app.html");
        if (!File.Exists(file))
            return Missing;
        var html = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        return html.Replace(Root, $"<div id=\"root\" data-app=\"{app}\"></div>", StringComparison.Ordinal);
    }
}
