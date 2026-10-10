using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Live;

/// <summary>
/// Sprava WebSocketu <c>/ws</c> - zmeny stavu simulatora pre prehliadace. Druh je vo vlastnosti <c>type</c>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StructureMessage), "structure")]
[JsonDerivedType(typeof(BoardMessage), "board")]
[JsonDerivedType(typeof(LineMessage), "line")]
[JsonDerivedType(typeof(FramesMessage), "frames")]
[JsonDerivedType(typeof(DesignMessage), "design")]
[JsonDerivedType(typeof(RecordingsMessage), "recordings")]
[JsonDerivedType(typeof(RecorderMessage), "recorder")]
[JsonDerivedType(typeof(ReplayMessage), "replay")]
[JsonDerivedType(typeof(VirtualMessage), "virtual")]
[JsonDerivedType(typeof(SignageChangedMessage), "signage")]
[JsonDerivedType(typeof(ScenariosMessage), "scenarios")]
[JsonDerivedType(typeof(ReportsMessage), "reports")]
public abstract record LiveMessage;
