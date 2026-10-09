namespace TableSimulator.Contracts.Recordings;

/// <summary>Porovnanie jednej tabule v dvoch zaznamoch.</summary>
/// <param name="Id">tabula <c>linka-adresa</c></param>
/// <param name="Name">nazov tabule (z prveho zaznamu, v ktorom je)</param>
/// <param name="Result">vysledok</param>
/// <param name="StatesA">pocet stavov v prvom zazname</param>
/// <param name="StatesB">pocet stavov v druhom zazname</param>
/// <param name="Steps">kroky porovnania v poradi</param>
public sealed record BoardComparisonDto(string Id, string Name, BoardDifference Result, int StatesA, int StatesB, IReadOnlyList<ComparisonStepDto> Steps);
