namespace TableSimulator.Contracts.Recordings;

/// <summary>Priebeh prehravania.</summary>
/// <param name="Position">kde v zazname prehravanie je [s]</param>
/// <param name="Duration">dlzka zaznamu [s]</param>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Pass">poradie prechodu pri opakovani</param>
/// <param name="At">kedy plati poloha - pocas prehravania sa da dopocitat (poloha + uplynuly cas × rychlost)</param>
public sealed record ReplayStatusDto(string Name, ReplayState State, double Position, double Duration, double Speed, bool Loop, int Pass, DateTimeOffset At);
