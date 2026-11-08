namespace TableSimulator.Core.Boards.Announcements;

/// <summary>Ticho pred a po nahravke hlasenia (vzdialeny INISS posiela v <c>RWAV</c> pri kazdej nahravke).</summary>
/// <param name="Before">ticho pred nahravkou [ms]</param>
/// <param name="After">ticho po nahravke [ms]</param>
public sealed record SoundPause(int Before, int After);
