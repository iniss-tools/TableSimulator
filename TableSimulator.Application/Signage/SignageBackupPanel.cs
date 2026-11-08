using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Panel v zalohe.</summary>
public sealed record SignageBackupPanel(int Id, SetSignagePanelRequest Definition);
