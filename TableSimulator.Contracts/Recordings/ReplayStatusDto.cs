namespace TableSimulator.Contracts.Recordings;

/// <summary>Priebeh prehravania.</summary>
/// <param name="Position">kde v zazname prehravanie je [s]</param>
/// <param name="Duration">dlzka zaznamu [s]</param>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Pass">poradie prechodu pri opakovani</param>
public sealed record ReplayStatusDto(string Name, ReplayState State, double Position, double Duration, double Speed, bool Loop, int Pass);
