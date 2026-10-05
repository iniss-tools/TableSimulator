using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Keys;

/// <summary>Druh pristupoveho kluca.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccessKeyKind>))]
public enum AccessKeyKind
{
    /// <summary>Zobrazovacie zariadenie (monitor, kiosk) - kluc v odkaze stranky, vzdy len na pozeranie.</summary>
    [JsonStringEnumMemberName("device")] Device,

    /// <summary>Program (GVDEditor, skript) - kluc v hlavicke <c>Authorization: Bearer</c>, rola podla volby.</summary>
    [JsonStringEnumMemberName("api")] Api
}
