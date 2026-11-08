using System.ComponentModel;
using TableSimulator.Contracts.AkSignal;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Display;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Mcp;

/// <summary>Ustredne AK Signal.</summary>
public sealed record AkSignalControllersResult(IReadOnlyList<AkSignalController> Controllers);

/// <summary>Ustredna AK Signal - svetlo, snimac, stavy a rozhlas tak, ako ich vidi server INISS2.</summary>
public sealed record AkSignalController(
    [property: Description("Board id \"line-address\" of the controller.")]
    string Id,
    string Name,
    [property: Description("AKSIGNAL (binary, class 131) or AKSIGNALUH (text, class 132).")]
    string Protocol,
    [property: Description("Simulated power outage: the controller does not answer.")]
    bool PoweredOff,
    [property: Description("The lights are on: switched on manually, or automatic mode while it is dark.")]
    bool LightOn,
    [property: Description("Light mode set by the server: off, auto (switched by trains) or manual.")]
    string LightMode,
    [property: Description("The twilight sensor reports darkness.")]
    bool Dark,
    [property: Description("Twilight sensor setting: sun (by sunrise and sunset at the simulator location), dark or light.")]
    TwilightMode Sensor,
    [property: Description("Sensor offset against sunrise and sunset [min]; positive shortens the darkness.")]
    int SensorOffsetMinutes,
    [property: Description("When the sensor changes by the sun; null = fixed sensor.")]
    DateTimeOffset? DarkChangesAt,
    [property: Description("Conditions the controller reports now: from its settings together with a running fault scenario.")]
    AkSignalConditionsDto Conditions,
    [property: Description("What a running fault scenario sets now (added conditions, sensor mode); null = nothing.")]
    ScenarioDeviceDto? Scenario,
    [property: Description("The server has not asked the controller for a while (SilenceSeconds).")]
    bool ServerSilent,
    [property: Description("Last query of the server; null = none yet.")]
    DateTimeOffset? LastQueryAt,
    [property: Description("Light state as the controller would reply now: hex byte (AKSIGNAL) or keys (AKSIGNALUH).")]
    string LightState,
    [property: Description("Audio state as the controller would reply now: hex byte (AKSIGNAL) or keys (AKSIGNALUH).")]
    string AudioState,
    [property: Description("Lights of the station (Lights.xml) the controller switches.")]
    IReadOnlyList<string> Lights,
    [property: Description("Audio lines of the station (Audio.xml) that announce through the controller.")]
    IReadOnlyList<string> AudioLines,
    [property: Description("Announcements playing now; channel 0 = AKSIGNAL switching, 1 and 2 = AKSIGNALUH channels.")]
    IReadOnlyList<AnnouncementDto> Playing);

/// <summary>Historia ustredni AK Signal za obdobie.</summary>
public sealed record AkSignalHistoryResult(
    DateTimeOffset From,
    DateTimeOffset To,
    [property: Description("Light states: the state at the start of the period and every change; the last ones when truncated.")]
    IReadOnlyList<LightStateDto> States,
    [property: Description("Finished announcements started in the period; the last ones when truncated.")]
    IReadOnlyList<AnnouncementHistoryDto> Announcements,
    [property: Description("Light log of the INISS2 server: why the server switched a light (train, manual, state); the last ones when truncated.")]
    IReadOnlyList<ServerLightEventDto> ServerEvents,
    [property: Description("Some list had more entries than the limit.")]
    bool Truncated);
