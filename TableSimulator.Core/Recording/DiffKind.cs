namespace TableSimulator.Core.Recording;

/// <summary>Druh kroku porovnania.</summary>
public enum DiffKind
{
    /// <summary>Rovnaky text v oboch zaznamoch.</summary>
    Same,

    /// <summary>Na tom istom mieste postupnosti iny text.</summary>
    Changed,

    /// <summary>Text len v prvom zazname.</summary>
    Removed,

    /// <summary>Text len v druhom zazname.</summary>
    Added
}
