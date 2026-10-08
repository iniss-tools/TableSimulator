using System.Text.Json;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Designs;

public sealed class SetDesignCommandHandler(AppPaths paths, ISettingsStore settings, ILiveNotifier live) : ICommandHandler<SetDesignCommand>
{
    public async Task<Result> Handle(SetDesignCommand command, CancellationToken cancellationToken)
    {
        if (DesignFolder.Prepare(paths, settings.Current) is not { } folder)
            return Result.Failure(DesignErrors.FolderMissing(paths.DesignsFolder(settings.Current)));
        var dir = Directory.CreateDirectory(Path.Combine(folder, command.Id));
        await File.WriteAllTextAsync(Path.Combine(dir.FullName, "design.json"), JsonSerializer.Serialize(command.Request, DesignFolder.Json) + Environment.NewLine,
            cancellationToken).ConfigureAwait(false);
        // sledovanie priecinka to ohlasi tiez - ak priecinok pri starte este nebol, len toto
        live.Publish(new DesignMessage(command.Id));
        return Result.Success();
    }
}
