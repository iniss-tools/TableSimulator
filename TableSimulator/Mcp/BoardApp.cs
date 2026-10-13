using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using TableSimulator.Application.Auth;

namespace TableSimulator.Mcp;

/// <summary>Zdroj MCP Apps so zivou tabulou - HTML, ktore klient ukaze v izolovanom ramci (vlozene do zostavy).</summary>
[McpServerResourceType]
public static class BoardApp
{
    public const string Uri = "ui://tablesimulator/board.html";

    private static readonly Lazy<string> Content = new(() =>
    {
        using var stream = typeof(BoardApp).Assembly.GetManifestResourceStream("TableSimulator.Mcp.board-app.html")
                           ?? throw new InvalidOperationException("The board app is missing in the assembly.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    [McpServerResource(UriTemplate = Uri, Name = "board-view", Title = "Board view", MimeType = McpApps.HtmlMimeType)]
    [Description("Live view of a board for show_board in clients with MCP Apps.")]
    [Authorize(Policy = Permissions.SimulatorView)]
    public static string Html() => Content.Value;
}
