using System.Text.Json.Serialization;

namespace TableSimulator.Core.Scenarios;

/// <summary>Co krok scenara robi.</summary>
public enum ScenarioAction
{
    /// <summary>Predstierana porucha tabule (<see cref="ScenarioStep.Fault" />).</summary>
    [JsonStringEnumMemberName("fault")] Fault,

    /// <summary>Vypadok napajania tabule - neodpoveda a po navrate je prazdna.</summary>
    [JsonStringEnumMemberName("powerLoss")] PowerLoss,

    /// <summary>Vypadok linky - spojenie s INISSom sa zavrie a nove sa neprijima.</summary>
    [JsonStringEnumMemberName("lineOutage")] LineOutage,

    /// <summary>Stavy a snimac ustredne AK Signal (<see cref="ScenarioStep.Device" />); inej tabule sa netyka.</summary>
    [JsonStringEnumMemberName("device")] Device
}
