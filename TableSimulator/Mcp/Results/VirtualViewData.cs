using System.ComponentModel;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Mcp.Results;

/// <summary>Udaje zobrazenia virtualnej tabule pre aplikaciu MCP Apps (web/src/mcp-app/VirtualApp.tsx).</summary>
public sealed record VirtualViewData(
    [property: Description("The content changed; false = the timeout expired.")]
    bool Changed,
    [property: Description("Hash of the content; pass it as afterHash to wait for the next change.")]
    string Hash,
    VirtualViewDto View);
