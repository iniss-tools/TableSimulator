using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Scenarios;

/// <summary>Co krok scenara robi.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ScenarioAction>))]
public enum ScenarioAction
{
    /// <summary>Predstierana porucha tabule.</summary>
    [JsonStringEnumMemberName("fault")] Fault,

    /// <summary>Vypadok napajania tabule - neodpoveda a po navrate je prazdna.</summary>
    [JsonStringEnumMemberName("powerLoss")] PowerLoss,

    /// <summary>Vypadok linky - spojenie s INISSom sa zavrie a nove sa neprijima.</summary>
    [JsonStringEnumMemberName("lineOutage")] LineOutage
}
