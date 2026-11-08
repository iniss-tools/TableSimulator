using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Application.Devices.Sounds;

/// <summary>Nahravka banky zvukov programu INISSu ako WAV na prehratie hlasenia v prehliadaci.</summary>
/// <param name="Name">meno, ako ho INISS posiela (server INISS2 <c>CZ/vlak/1234</c>, INISS 3.39 <c>jazyk/skupina/zvuk</c>)</param>
public sealed record GetSoundQuery(string Name) : IQuery<byte[]>;

public sealed class GetSoundQueryHandler(SoundBanks banks) : IQueryHandler<GetSoundQuery, byte[]>
{
    public async Task<Result<byte[]>> Handle(GetSoundQuery query, CancellationToken cancellationToken)
    {
        if (banks.Current is not { } bank)
            return Result.Failure<byte[]>(SoundErrors.NoBank);
        var name = query.Name.Trim();
        if (name.Length == 0 || bank.FileOf(name) is not { } file)
            return Result.Failure<byte[]>(SoundErrors.NotFound(name));

        try
        {
            using var ewa = new MemoryStream(await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false));
            using var wav = new MemoryStream();
            EwaCodec.Decode(ewa, wav);
            var data = wav.ToArray();
            WavHeader.ClampData(data);
            return Result.Success(data);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<byte[]>(SoundErrors.Unreadable(e.Message));
        }
    }
}
