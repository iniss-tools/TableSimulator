namespace TableSimulator.Core.Simulation;

/// <summary>
/// Odkial prisli bajty linky.
/// </summary>
public enum TrafficSource
{
    /// <summary>Od INISSu po linke.</summary>
    Line,

    /// <summary>Z prehravaneho zaznamu - nezapisuje sa do prebiehajuceho zaznamu.</summary>
    Replay
}
