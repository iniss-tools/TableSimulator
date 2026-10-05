using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

/// <summary>Ulozi zaznam z ineho pocitaca.</summary>
/// <param name="FileName">meno suboru z prehliadaca (podla neho sa zaznam pomenuje)</param>
/// <param name="Content">obsah suboru</param>
public sealed record UploadRecordingCommand(string FileName, byte[] Content) : ICommand<RecordingInfoDto>;
