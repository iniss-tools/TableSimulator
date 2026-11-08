namespace TableSimulator.Core.Recording;

/// <summary>
/// Priebeh prehravania pre rozhranie.
/// </summary>
/// <param name="Name">meno zaznamu</param>
/// <param name="State">stav</param>
/// <param name="Position">kde v zazname prehravanie je [s]</param>
/// <param name="Duration">dlzka zaznamu [s]</param>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Loop">po skonceni zacat znova</param>
/// <param name="Pass">kolke prehratie (pri opakovani)</param>
/// <param name="At">kedy plati poloha - pocas prehravania sa da dopocitat (poloha + uplynuly cas × rychlost)</param>
public sealed record ReplayStatus(string Name, ReplayState State, double Position, double Duration, double Speed, bool Loop, int Pass, DateTimeOffset At);
