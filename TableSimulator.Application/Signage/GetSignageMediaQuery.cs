using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Signage;

/// <summary>Subor obrazka alebo videa spravy.</summary>
public sealed record GetSignageMediaQuery(Guid Id) : IQuery<SignageMediaFile>;

/// <summary>Cesta k suboru a typ obsahu.</summary>
public sealed record SignageMediaFile(string Path, string ContentType);

public sealed class GetSignageMediaQueryHandler(ISignageRepository repository, SignageMediaStore store) : IQueryHandler<GetSignageMediaQuery, SignageMediaFile>
{
    public async Task<Result<SignageMediaFile>> Handle(GetSignageMediaQuery query, CancellationToken cancellationToken)
    {
        if (await repository.FindMediaAsync(query.Id, cancellationToken).ConfigureAwait(false) is not { } media)
            return Result.Failure<SignageMediaFile>(SignageErrors.MediaNotFound());
        var path = store.PathOf(media.FileName);
        return File.Exists(path) ? Result.Success(new SignageMediaFile(path, media.ContentType)) : Result.Failure<SignageMediaFile>(SignageErrors.MediaNotFound());
    }
}
