using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Prehlad simulatora pre model: linky a tabule s problemom.</summary>
public sealed record StatusResult(
    IReadOnlyList<LineSummary> Lines,
    [property: Description("Boards with a simulated fault, power outage or manufacturer mismatch; empty when all boards are fine.")]
    IReadOnlyList<BoardIssue> Issues);
