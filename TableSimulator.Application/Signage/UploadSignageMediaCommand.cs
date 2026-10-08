using System.Text.Json.Serialization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Nahra obrazok alebo video pre spravu (obsah do auditu nejde).</summary>
public sealed record UploadSignageMediaCommand(string Name, [property: JsonIgnore] Stream Content) : ICommand<SignageMediaDto>, IAuditable
{
    string IAuditable.AuditTarget => Name;
}

public sealed class UploadSignageMediaCommandValidator : AbstractValidator<UploadSignageMediaCommand>
{
    public UploadSignageMediaCommandValidator()
    {
        RuleFor(c => c.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(200).WithErrorCode(SignageErrors.MediaName);
        RuleFor(c => c.Name).Must(n => SignageMediaStore.Types.ContainsKey(Path.GetExtension(n ?? ""))).WithErrorCode("signage.mediaType")
            .WithMessage("Supported files: PNG, JPEG, GIF, WebP, MP4, WebM.");
    }
}

public sealed class UploadSignageMediaCommandHandler(ISignageRepository repository, SignageMediaStore store, TimeProvider time)
    : ICommandHandler<UploadSignageMediaCommand, SignageMediaDto>
{
    /// <summary>Subor, ktory po dni nepouziva ziadna sprava, sa odstrani (nahraty a neulozeny).</summary>
    private static readonly TimeSpan Orphaned = TimeSpan.FromDays(1);

    public async Task<Result<SignageMediaDto>> Handle(UploadSignageMediaCommand command, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(command.Name).ToLowerInvariant();
        var id = Guid.NewGuid();
        var fileName = id.ToString("N") + extension;
        if (await store.WriteAsync(fileName, command.Content, cancellationToken).ConfigureAwait(false) is not { } size)
            return Result.Failure<SignageMediaDto>(SignageErrors.MediaTooLarge());

        var now = time.GetUtcNow();
        var media = new SignageMediaRecord(id, Path.GetFileName(command.Name.Trim()), SignageMediaStore.Types[extension], size, fileName, now);
        await repository.AddMediaAsync(media, cancellationToken).ConfigureAwait(false);
        await RemoveOrphansAsync(now, cancellationToken).ConfigureAwait(false);
        return Result.Success(new SignageMediaDto(media.Id, media.Name, media.ContentType, media.Size));
    }

    private async Task RemoveOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
        var used = data.Messages.Values.Select(m => m.Definition.Media).OfType<Guid>().ToHashSet();
        var orphans = data.Media.Values.Where(m => !used.Contains(m.Id) && now - m.CreatedAt > Orphaned).ToList();
        if (orphans.Count == 0)
            return;
        await repository.DeleteMediaAsync([.. orphans.Select(m => m.Id)], cancellationToken).ConfigureAwait(false);
        store.Delete(orphans);
    }
}
