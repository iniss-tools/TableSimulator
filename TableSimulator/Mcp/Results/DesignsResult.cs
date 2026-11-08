using System.ComponentModel;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Mcp.Results;

/// <summary>Vzhlady tabul.</summary>
public sealed record DesignsResult(
    [property: Description("Designs; renderer led-matrix = dots (LED and LCD), flaps = split-flap boards.")]
    IReadOnlyList<DesignInfoDto> Designs);
