using System.ComponentModel;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Mcp.Results;

/// <summary>Vzhlad tabule po zmene.</summary>
public sealed record BoardLookResult(
    string Id,
    [property: Description("Design set for the board; null = default for the manufacturer.")]
    string? Design,
    [property: Description("Design actually used: the board's design, or the default design of the manufacturer when none is set or it does not exist.")]
    string DesignInUse,
    BoardProfileDto Profile,
    [property: Description("Page showing only this board; open it in a browser to see the result (with login enabled the page needs signing in).")]
    string ViewUrl);
