namespace TableSimulator.Contracts.Display;

/// <summary>Ticho pred a po nahravke hlasenia.</summary>
/// <param name="BeforeMs">ticho pred nahravkou [ms]</param>
/// <param name="AfterMs">ticho po nahravke [ms]</param>
public sealed record SoundPauseDto(int BeforeMs, int AfterMs);
