namespace TableSimulator.Contracts.Archive;

/// <summary>Vlak zaznamu archivu.</summary>
/// <param name="Date">datum vlaku, ak sa lisi od dna zaznamu</param>
public sealed record ArchiveTrainDto(string Type, string Number, string Name, DateOnly? Date);
