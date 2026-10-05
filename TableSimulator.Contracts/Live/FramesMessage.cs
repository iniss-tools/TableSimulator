using TableSimulator.Contracts.Frames;

namespace TableSimulator.Contracts.Live;

/// <summary>Nove zaznamy komunikacie - len klientom, ktori o ne poziadali (<c>/ws?log=1</c>).</summary>
public sealed record FramesMessage(IReadOnlyList<FrameDto> Frames) : LiveMessage;
