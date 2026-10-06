namespace TableSimulator.Application.Config;

/// <summary>Linky a tabule na stiahnutie.</summary>
/// <param name="Content">JSON</param>
/// <param name="FileName">meno suboru</param>
public sealed record ConfigFile(string Content, string FileName);
