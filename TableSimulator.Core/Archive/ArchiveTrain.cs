namespace TableSimulator.Core.Archive;

/// <summary>Vlak zaznamu archivu.</summary>
/// <param name="Type">druh (<c>t</c>)</param>
/// <param name="Number">cislo (<c>v</c>)</param>
/// <param name="Name">nazov (<c>jm</c>)</param>
/// <param name="Date">datum vlaku (<c>dt</c>), ak sa lisi od dna zaznamu</param>
public sealed record ArchiveTrain(string Type, string Number, string Name, DateOnly? Date);
