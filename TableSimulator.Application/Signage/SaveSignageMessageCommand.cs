using System.Globalization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Live;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Zalozi (<paramref name="Id" /> null) alebo zmeni spravu.</summary>
public sealed record SaveSignageMessageCommand(int? Id, SetSignageMessageRequest Request) : ICommand<SignageCreatedDto>, IAuditable
{
    string IAuditable.AuditTarget => Id?.ToString(CultureInfo.InvariantCulture) ?? Request.Name;
}

public sealed class SaveSignageMessageCommandValidator : AbstractValidator<SaveSignageMessageCommand>
{
    public SaveSignageMessageCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).When(c => c.Id is not null).WithErrorCode(SignageErrors.Id);
        RuleFor(c => c.Request).NotNull().WithErrorCode(SignageErrors.Name).SetValidator(new SignageMessageRules());
    }
}

public sealed class SaveSignageMessageCommandHandler(ISignageRepository repository, IVirtualBoardRepository virtualBoards, ILiveNotifier live)
    : ICommandHandler<SaveSignageMessageCommand, SignageCreatedDto>
{
    public async Task<Result<SignageCreatedDto>> Handle(SaveSignageMessageCommand command, CancellationToken cancellationToken)
    {
        var request = Normalize(command.Request);
        if (request.Kind == SignageMessageKind.Virtual && await virtualBoards.FindAsync(request.VirtualBoard!.Value, cancellationToken).ConfigureAwait(false) is null)
            return Result.Failure<SignageCreatedDto>(SignageErrors.UnknownVirtual(request.VirtualBoard.Value));
        if (request.Kind is SignageMessageKind.Image or SignageMessageKind.Video)
        {
            if (await repository.FindMediaAsync(request.Media!.Value, cancellationToken).ConfigureAwait(false) is not { } media)
                return Result.Failure<SignageCreatedDto>(SignageErrors.UnknownMedia());
            if (SignageMediaStore.IsVideo(media.ContentType) != (request.Kind == SignageMessageKind.Video))
                return Result.Failure<SignageCreatedDto>(SignageErrors.MediaKind());
        }

        var id = await repository.SaveMessageAsync(new SignageMessageRecord(command.Id ?? 0, request.Name, SignageJson.Serialize(request)), cancellationToken)
            .ConfigureAwait(false);
        if (id == 0)
            return Result.Failure<SignageCreatedDto>(SignageErrors.NotFound(SignageEntity.Message, command.Id ?? 0));
        live.Publish(new SignageChangedMessage());
        return Result.Success(new SignageCreatedDto(id));
    }

    /// <summary>Len cast spravy podla druhu, texty bez okrajovych medzier.</summary>
    private static SetSignageMessageRequest Normalize(SetSignageMessageRequest r) => r with
    {
        Name = r.Name.Trim(),
        Note = r.Note.Trim(),
        VirtualBoard = r.Kind == SignageMessageKind.Virtual ? r.VirtualBoard : null,
        Board = r.Kind == SignageMessageKind.Board ? r.Board : null,
        Paragraphs = r.Kind == SignageMessageKind.Text ? r.Paragraphs : [],
        Media = r.Kind is SignageMessageKind.Image or SignageMessageKind.Video ? r.Media : null,
        Url = r.Kind == SignageMessageKind.Web ? r.Url!.Trim() : null
    };
}
