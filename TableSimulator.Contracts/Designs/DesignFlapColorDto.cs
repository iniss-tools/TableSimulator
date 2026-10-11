namespace TableSimulator.Contracts.Designs;

/// <summary>Farby jedneho listu.</summary>
/// <param name="Top">horna polovica</param>
/// <param name="Bottom">dolna polovica</param>
/// <param name="Text">text</param>
public sealed record DesignFlapColorDto(string Top, string Bottom, string Text);
