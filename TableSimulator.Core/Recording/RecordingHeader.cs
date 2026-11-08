using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Hlavicka zaznamu: kedy vznikol a nastavenie liniek a tabul v tom case (pri prehrati inde sa podla neho doplnia).
/// </summary>
/// <param name="Format">vzdy <see cref="RecordingFile.FormatName" /></param>
/// <param name="Version">verzia formatu</param>
/// <param name="Created">zaciatok zaznamu</param>
/// <param name="Config">linky a tabule simulatora pri zaciatku zaznamu</param>
/// <param name="Note">poznamka obsluhy</param>
public sealed record RecordingHeader(string Format, int Version, DateTimeOffset Created, SimulatorConfig Config, string? Note);
