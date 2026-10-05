namespace TableSimulator.Contracts.Archive;

/// <summary>Dalsi atribut zaznamu archivu hlaseni (napr. <c>vyl</c> s textom z legendy vyluk).</summary>
public sealed record ArchiveDetailDto(string Key, string Value);
